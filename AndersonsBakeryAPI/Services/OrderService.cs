using SharedLibrary.Interface;
using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using AndersonsBakeryAPI.Repositories;
using AndersonsBakeryAPI.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace AndersonsBakeryAPI.Services
{
    public class OrderService : IOrderService
    {
        // Initialise lists and repositories for order processing
        private readonly IProductService _productService;
        private readonly MongoOrderRepository? _mongoRepository;
        private readonly SqlOrderRepository? _sqlRepository;
        private static readonly List<OrderScaled> _orders = new();
        private static bool _localLoaded = false;

        //------------------------------------------------------------------------------------------------//

        // Returns the file path for storing local orders in a JSON file
        private static string OrdersFilePath()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            return Path.Combine(dataFolder, "orders.json");
        }

        //------------------------------------------------------------------------------------------------//

        // Loads orders from the local JSON file into memory, ensuring no duplicates are added
        private static void LoadLocalOrders()
        {
            try
            {
                // Load orders from the local JSON file if it exists
                string path = OrdersFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var items = JsonSerializer.Deserialize<List<OrderScaled>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (items != null)
                    {
                        lock (_orders)
                        {
                            foreach (var item in items)
                            {
                                if (!_orders.Any(o => o.OrderId.Equals(item.OrderId, StringComparison.OrdinalIgnoreCase)))
                                {
                                    _orders.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCAL] Error loading local orders.json: {ex.Message}");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Saves the current in-memory orders to the local JSON file, ensuring thread safety
        private static void SaveLocalOrders()
        {
            try
            {
                // Save the current in-memory orders to the local JSON file
                string path = OrdersFilePath();
                string json;
                lock (_orders)
                {
                    json = JsonSerializer.Serialize(_orders, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCAL] Error saving local orders.json: {ex.Message}");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Creates a default SQL repository for order storage, connecting to a local SQL Server database
        private static SqlOrderRepository? CreateDefaultSqlRepository()
        {
            try
            {
                // Configure the DbContextOptionsBuilder to connect to a local SQL Server database
                var optionsBuilder = new DbContextOptionsBuilder<LogiSynDbContext>();
                optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;TrustServerCertificate=True;");
                var context = new LogiSynDbContext(optionsBuilder.Options);
                context.Database.EnsureCreated();
                return new SqlOrderRepository(context);
            }
            // Catch any exceptions that occur during the creation of the SQL repository and log the error
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL] Could not initialize SQL repository: {ex.Message}");
                return null;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Creates a default MongoDB repository for order storage, connecting to a MongoDB Atlas cluster
        // NOTE: Ensure that the connection string is valid and that the MongoDB server is accessible
        private static MongoOrderRepository? CreateDefaultMongoRepository()
        {
            try
            {
                string? conn = MongoConfiguration.TryGetConnectionString();
                if (string.IsNullOrWhiteSpace(conn)) return null;

                string dbName = MongoConfiguration.GetDatabaseName();
                var settings = MongoClientSettings.FromConnectionString(conn);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
                settings.ConnectTimeout = TimeSpan.FromSeconds(2);
                var client = new MongoClient(settings);
                var db = client.GetDatabase(dbName);
                return new MongoOrderRepository(db);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGODB] Could not initialize MongoDB repository: {ex.Message}");
                return null;
            }
        }
        //------------------------------------------------------------------------------------------------//

        // Static constructor to load local orders when the class is first accessed
        static OrderService()
        {
            LoadLocalOrders();
        }

        //------------------------------------------------------------------------------------------------//

        // constructors for OrderService, allowing for dependency injection of ProductService and repositories
        public OrderService() : this(new ProductService(), CreateDefaultMongoRepository(), CreateDefaultSqlRepository()) { }

        public OrderService(IProductService productService) : this(productService, CreateDefaultMongoRepository(), CreateDefaultSqlRepository()) { }

        public OrderService(MongoOrderRepository? mongoRepository, SqlOrderRepository? sqlRepository)
            : this(new ProductService(), mongoRepository, sqlRepository) { }

        //------------------------------------------------------------------------------------------------//

        // Constructor that initializes the OrderService with a ProductService and optional repositories for MongoDB and SQL Server
        public OrderService(IProductService productService, MongoOrderRepository? mongoRepository, SqlOrderRepository? sqlRepository)
        {
            // Use the provided ProductService or create a new one if null
            _productService = productService ?? new ProductService();
            _mongoRepository = mongoRepository;
            _sqlRepository = sqlRepository;

            if (!_localLoaded)
            {
                LoadLocalOrders();
                _localLoaded = true;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Parse the PDF, extract order details, and scale the ingredients based on the recipes
        public OrderScaled ReadAndScaleOrder(string pdfPath, string fallbackOrderId = "#001")
        {
            // Validate the PDF file path
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("PDF file not found.", pdfPath);

            var extractedLines = ReadPdfLines(pdfPath).ToList();

            // Extract metadata from header
            string parsedOrderId = ExtractField(extractedLines, @"Order\s*(?:Number|#)?[:\s]*([#A-Za-z0-9\-]+)");
            string parsedCustomer = ExtractCustomer(extractedLines);
            DateTime? parsedDate = ExtractOrderDate(extractedLines);

            // Determine the order ID to use, prioritizing the parsed ID but falling back to a default if necessary
            string rawOrderId = !string.IsNullOrWhiteSpace(parsedOrderId) ? parsedOrderId : fallbackOrderId;

            // Itterate to ensure the order ID is unique within the current list of orders
            string uniqueOrderId = EnsureUniqueOrderId(rawOrderId);

            // Create the order object with extracted and default values
            var order = new OrderScaled
            {
                OrderId = uniqueOrderId,
                Customer = !string.IsNullOrWhiteSpace(parsedCustomer) ? parsedCustomer : "Spar",
                OrderDate = parsedDate ?? DateTime.Now,
                Status = "Pending",
                productionItems = new List<ProductionItem>()
            };

            // Loop through lines to match products from ProductService
            var availableProducts = _productService.GetAllProducts();

            for (int i = 0; i < extractedLines.Count; i++)
            {
                string line = extractedLines[i];

                if (IsDividerLine(line)) continue;

                var product = availableProducts
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ProductName) &&
                                         line.IndexOf(p.ProductName, StringComparison.OrdinalIgnoreCase) >= 0);

                if (product != null)
                {
                    // Extract quantity from the current line and the next two lines for context
                    string context = line;
                    for (int offset = 1; offset <= 2 && (i + offset) < extractedLines.Count; offset++)
                    {
                        if (!IsDividerLine(extractedLines[i + offset]))
                        {
                            context += " " + extractedLines[i + offset];
                        }
                    }

                    int quantity = ExtractQuantity(context);

                    string cleanProdName = product.ProductName ?? string.Empty;
                    var qtyMatch = Regex.Match(cleanProdName, @"^(\d+)\s+(.+)$");
                    if (qtyMatch.Success)
                    {
                        cleanProdName = qtyMatch.Groups[2].Value.Trim();
                    }

                    // Create a production item based on the product and calculated quantity
                    var item = new ProductionItem
                    {
                        ProductName = cleanProdName,
                        Amount = quantity,
                        ProductionLine = !string.IsNullOrWhiteSpace(product.StorageLocation) ? product.StorageLocation : "Production 1",
                        packaging = new Packaging
                        {
                            Pans = (int)Math.Ceiling((double)quantity / 50),
                            Trolleys = (int)Math.Ceiling((double)quantity / 100),
                            PansUsed = 0,
                            TrolleysUsed = 0
                        }
                    };

                    // Scale and add the required ingredients for this production item
                    if (product.Ingredients != null && product.Ingredients.Count > 0)
                    {
                        foreach (var ing in product.Ingredients)
                        {
                            double baseQty = (double)ing.Quantity;
                            item.ReqIngredients.Add(new Ingredients
                            {
                                IngredientName = ing.IngredientName,
                                IngredientAmount = Math.Round(baseQty * quantity, 1),
                                AdditionsAmount = Math.Round(baseQty * 0.1 * quantity, 1),
                                MeasuredIngredient = ing.Unit ?? "kg",
                                AmountUsed = 0
                            });
                        }
                    }
                    else
                    {
                        item.ReqIngredients.Add(new Ingredients
                        {
                            IngredientName = "Flour",
                            IngredientAmount = Math.Round(0.04 * quantity, 1),
                            AdditionsAmount = Math.Round(0.005 * quantity, 1),
                            MeasuredIngredient = "kg",
                            AmountUsed = 0
                        });
                        item.ReqIngredients.Add(new Ingredients
                        {
                            IngredientName = "Yeast",
                            IngredientAmount = Math.Round(0.01 * quantity, 1),
                            AdditionsAmount = Math.Round(0.001 * quantity, 1),
                            MeasuredIngredient = "kg",
                            AmountUsed = 0
                        });
                    }

                    order.productionItems.Add(item);
                }
            }

            // Calculate raw materials scaling totals
            RecalRawMaterials(order);

            return order;
        }

        //------------------------------------------------------------------------------------------------//

        // Ensures that the generated order ID is unique by checking against existing orders and incrementing a numeric suffix if necessary
        public string EnsureUniqueOrderId(string baseOrderId)
        {
            // If the base order ID is null or whitespace, default to "#001"
            if (string.IsNullOrWhiteSpace(baseOrderId))
                baseOrderId = "#001";

            string candidateId = baseOrderId;
            var existingOrderIds = new HashSet<string>(GetOrders().Select(o => o.OrderId), StringComparer.OrdinalIgnoreCase);

            // Check if the order ID is purely numeric (with optional '#' prefix)
            var numMatch = Regex.Match(baseOrderId, @"^(#?)(\d+)$");
            if (numMatch.Success)
            {
                string prefix = numMatch.Groups[1].Value;
                string digits = numMatch.Groups[2].Value;
                int num = int.Parse(digits);
                int padLength = digits.Length;

                // Increment the numeric part until a unique order ID is found
                while (existingOrderIds.Contains(candidateId))
                {
                    num++;
                    candidateId = $"{prefix}{num.ToString().PadLeft(padLength, '0')}";
                }

                return candidateId;
            }

            // If the order ID is not purely numeric, Add a suffix to ensure a unique order ID
            int suffix = 1;
            while (existingOrderIds.Contains(candidateId))
            {
                candidateId = $"{baseOrderId}-{suffix}";
                suffix++;
            }

            return candidateId;
        }

        //------------------------------------------------------------------------------------------------//

        // Recalculates the total raw materials required for an order by aggregating ingredient amounts across all production items
        public void RecalRawMaterials(OrderScaled order)
        {
            if (order == null) return;

            // Initialize a dictionary to hold the aggregated ingredient amounts and their units, using case-insensitive keys
            var aggregates = new Dictionary<string, (double Amount, string Unit)>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in order.productionItems)
            {
                // Iterate through each required ingredient in the production item
                foreach (var ing in item.ReqIngredients)
                {
                    double totalRequired = ing.IngredientAmount + ing.AdditionsAmount;

                    // Aggregate the ingredient amounts, summing them if the ingredient already exists in the dictionary
                    if (aggregates.TryGetValue(ing.IngredientName, out var existing))
                    {
                        aggregates[ing.IngredientName] = (existing.Amount + totalRequired, ing.MeasuredIngredient);
                    }
                    else
                    {
                        aggregates[ing.IngredientName] = (totalRequired, ing.MeasuredIngredient);
                    }
                }
            }

            order.RawMaterials = aggregates.ToDictionary(kvp => kvp.Key, kvp => new RawMaterialValue(kvp.Value.Amount, kvp.Value.Unit));
        }

        //------------------------------------------------------------------------------------------------//

        // Helper to merge fetched orders into the in-memory cache without duplicates
        private static void MergeOrders(IEnumerable<OrderScaled> incomingOrders)
        {
            lock (_orders)
            {
                foreach (var incoming in incomingOrders)
                {
                    int index = _orders.FindIndex(o => o.OrderId.Equals(incoming.OrderId, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        _orders[index] = incoming;
                    }
                    else
                    {
                        _orders.Add(incoming);
                    }
                }
            }
        }

        // Asynchronously fetches orders from MongoDB, falling back to SQL Server, then local memory
        public async Task<IEnumerable<OrderScaled>> GetOrdersAsync(bool forceRefresh = false)
        {
            // If cache is populated and fresh fetch is not forced, return immediate in-memory copy
            if (!forceRefresh)
            {
                lock (_orders)
                {
                    if (_orders.Count > 0)
                        return _orders.ToList();
                }
            }

            // 1. Attempt fetch from MongoDB
            if (_mongoRepository != null)
            {
                try
                {
                    var mongoOrders = (await _mongoRepository.GetAllOrdersAsync()).ToList();
                    if (mongoOrders.Count > 0)
                    {
                        MergeOrders(mongoOrders);
                        SaveLocalOrders();
                        return mongoOrders;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OrderService] Error fetching orders from MongoDB: {ex.Message}");
                }
            }

            // 2. Fallback: Attempt fetch from SQL Server
            if (_sqlRepository != null)
            {
                try
                {
                    var sqlOrders = (await _sqlRepository.GetAllOrdersAsync()).ToList();
                    if (sqlOrders.Count > 0)
                    {
                        MergeOrders(sqlOrders);
                        SaveLocalOrders();
                        return sqlOrders;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OrderService] Error fetching orders from SQL: {ex.Message}");
                }
            }

            // 3. Fallback: Return in-memory list (includes orders from Data/orders.json)
            lock (_orders)
            {
                return _orders.ToList();
            }
        }

        // Synchronous wrapper
        public IEnumerable<OrderScaled> GetOrders()
        {
            lock (_orders)
            {
                if (_orders.Count > 0)
                    return _orders.ToList();
            }

            try
            {
                return Task.Run(async () => await GetOrdersAsync(forceRefresh: false)).GetAwaiter().GetResult();
            }
            catch
            {
                lock (_orders) { return _orders.ToList(); }
            }
        }

        // Retrieves only completed orders for history purposes
        public IEnumerable<OrderScaled> GetHistory()
        {
            return GetOrders().Where(o =>
                string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Asynchronously retrieves an order by ID
        public async Task<OrderScaled?> GetOrderByIdAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return null;

            var all = await GetOrdersAsync(forceRefresh: false);
            return all.FirstOrDefault(o => o.OrderId.Equals(orderId, StringComparison.OrdinalIgnoreCase));
        }

        // Synchronous retrieval by ID
        public OrderScaled? GetOrderById(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return null;
            return GetOrders().FirstOrDefault(o => o.OrderId.Equals(orderId, StringComparison.OrdinalIgnoreCase));
        }

        // Asynchronously saves or updates an order locally and persists across MongoDB and SQL Server
        public async Task<OrderScaled> SaveOrderAsync(OrderScaled order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            lock (_orders)
            {
                _orders.RemoveAll(o => o.OrderId.Equals(order.OrderId, StringComparison.OrdinalIgnoreCase));
                _orders.Add(order);
            }

            // Persist locally to Data/orders.json
            SaveLocalOrders();

            // Persist to MongoDB
            if (_mongoRepository != null)
            {
                try
                {
                    await _mongoRepository.SaveOrderAsync(order);
                    Console.WriteLine($"[MONGODB] Successfully saved order {order.OrderId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONGODB] Error saving order to MongoDB: {ex.Message}");
                }
            }

            // Persist to SQL Server
            if (_sqlRepository != null)
            {
                try
                {
                    await _sqlRepository.SaveOrderAsync(order);
                    Console.WriteLine($"[SQL] Successfully saved order {order.OrderId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SQL] Error saving order to SQL: {ex.Message}");
                }
            }

            return order;
        }

        // Synchronous save wrapper
        public void SaveOrder(OrderScaled order)
        {
            if (order == null) return;
            try
            {
                Task.Run(async () => await SaveOrderAsync(order)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrderService] Error in sync SaveOrder: {ex.Message}");
            }
        }

        // Completes an order asynchronously
        public async Task CompleteOrderAsync(string orderId, Action<OrderScaled>? recordOrderData = null)
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null) return;

            recordOrderData?.Invoke(order);
            order.Status = "Completed";
            await SaveOrderAsync(order);
        }

        public async Task CompleteOrderAsync(string orderId, OrderScaled completedOrder)
        {
            if (completedOrder == null) throw new ArgumentNullException(nameof(completedOrder));
            completedOrder.OrderId = orderId;
            completedOrder.Status = "Completed";
            await SaveOrderAsync(completedOrder);
        }

        // Synchronous complete wrapper
        public void CompleteOrder(string orderId, Action<OrderScaled>? recordOrderData = null)
        {
            var order = GetOrderById(orderId);
            if (order == null) return;

            recordOrderData?.Invoke(order);
            order.Status = "Completed";
            SaveOrder(order);
        }

        public void CompleteOrder(string orderId, OrderScaled completedOrder)
        {
            if (completedOrder == null) return;
            completedOrder.OrderId = orderId;
            completedOrder.Status = "Completed";
            SaveOrder(completedOrder);
        }


        //------------------------------------------------------------------------------------------------//

        // Helper method to read all lines of text from a PDF file using PdfPig
        private IEnumerable<string> ReadPdfLines(string pdfPath)
        {
            // Validate the PDF file path
            var lines = new List<string>();

            // Use PdfPig to open the PDF and extract text from each page
            using var doc = PdfDocument.Open(pdfPath);
            foreach (var page in doc.GetPages())
            {
                var text = page.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Split the page text into lines, trimming whitespace and ignoring empty lines
                var pageLines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var l in pageLines)
                {
                    var trimmed = l.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        lines.Add(trimmed);
                    }
                }
            }

            return lines;
        }

        //------------------------------------------------------------------------------------------------//

        // Helper method to extract the customer name from the PDF lines, stopping at subsequent labels
        private string ExtractCustomer(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                // Matches Client name formats accordingly
                var match = Regex.Match(
                    line,
                    @"Customer\s*[:\-]\s*([A-Za-z0-9&'\.\s\-]+?)(?=\s+(?:Order|Date|Delivery|Vendor|Line|#)|$)",
                    RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string extracted = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(extracted))
                        return extracted;
                }
            }
            return string.Empty;
        }

        //------------------------------------------------------------------------------------------------//

        private string ExtractField(IEnumerable<string> lines, string regexPattern)
        {
            foreach (var line in lines)
            {
                var match = Regex.Match(line, regexPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
            return string.Empty;
        }

        //------------------------------------------------------------------------------------------------//

        // Helper method to extract the order date from the PDF lines, supporting multiple formats
        private DateTime? ExtractOrderDate(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                // Check for explicit "Order Date" label and parse the date
                if (line.StartsWith("Order Date", StringComparison.OrdinalIgnoreCase))
                {
                    var clean = line.Replace("Order Date:", "").Replace("Order Date", "").Trim();
                    if (DateTime.TryParse(clean, out var dt)) return dt;
                }

                // Check for common date formats like "12 Jan 2024" or "12/01/2024"
                var match = Regex.Match(line, @"\b\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{4}\b", RegexOptions.IgnoreCase);
                if (match.Success && DateTime.TryParse(match.Value, out var parsed))
                {
                    return parsed;
                }

                // Check for numeric date formats like "12/01/2024" or "12-01-2024"
                var numDateMatch = Regex.Match(line, @"\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b");
                if (numDateMatch.Success && DateTime.TryParse(numDateMatch.Value, out var dtNum))
                {
                    return dtNum;
                }
            }
            return null;
        }

        //------------------------------------------------------------------------------------------------//

        // Helper method to extract quantity from a line of text, with fallback to the last number found
        private int ExtractQuantity(string text)
        {
            // Matches "pkts 250", "pkts | 110", or "pkts: 45"
            var pktMatch = Regex.Match(text, @"(?:pkts\s*\|?\s*|Quantity\s*\|?\s*)(\d{1,4})\b", RegexOptions.IgnoreCase);
            if (pktMatch.Success && int.TryParse(pktMatch.Groups[1].Value, out int pktQty))
            {
                return pktQty;
            }

            // 2. Matches "Qty: 100", "Qty | 200", or "Qty 300"
            var allNumbers = Regex.Matches(text, @"\b(\d{2,4})\b");
            if (allNumbers.Count > 0)
            {
                if (int.TryParse(allNumbers[allNumbers.Count - 1].Value, out int fallbackQty))
                    return fallbackQty;
            }

            return 100;
        }

        //------------------------------------------------------------------------------------------------//

        // Helper method to identify divider lines in the PDF text
        private bool IsDividerLine(string line)
        {
            return line.Trim().All(c => c == '=' || c == '-' || c == '_' || c == '*' || char.IsWhiteSpace(c));
        }

        //------------------------------------------------------------------------------------------------//

        // Email template for order, sent to managers
        public EmailMessageModel BuildScalingSheetEmail(OrderScaled order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string subject = $"Scaling Sheet - {order.Customer} Order {order.OrderId}";

            // Build the email body with order details, production items, and raw material totals
            var sb = new StringBuilder();
            sb.AppendLine("LOGISYN PRODUCTION SCALING SHEET");
            sb.AppendLine($"Order:     {order.Customer} Order {order.OrderId}");
            sb.AppendLine($"Date:      {order.OrderDate:d MMMM yyyy}");
            sb.AppendLine($"Status:    {order.Status}");
            sb.AppendLine();

            sb.AppendLine("==========================================");
            sb.AppendLine("PRODUCTION ITEMS & PACKAGING");
            sb.AppendLine("==========================================");

            // Loop through each production item and append its details to the email body
            foreach (var item in order.productionItems)
            {
                sb.AppendLine($"• {item.ProductName}  [{item.ProductionLine}]");

                var ingSummary = string.Join(", ", item.ReqIngredients.Select(i =>
                    $"{i.IngredientName}: {i.IngredientAmount + i.AdditionsAmount} {i.MeasuredIngredient}"));

                sb.AppendLine($"  - Ingredients: {ingSummary}");
                sb.AppendLine($"  - Packaging:   Pans: {item.packaging.Pans}, Trolleys: {item.packaging.Trolleys}");
                sb.AppendLine();
            }

            // Append raw material scaling totals to the email body
            sb.AppendLine("==========================================");
            sb.AppendLine("RAW MATERIAL SCALING (TOTALS)");
            sb.AppendLine("==========================================");

            foreach (var kvp in order.RawMaterials)
            {
                sb.AppendLine($"• {kvp.Key}: {kvp.Value.Amount} {kvp.Value.Unit}");
            }

            sb.AppendLine();
            sb.AppendLine("------------------------------------------");
            sb.AppendLine("Generated by LogiSyn Bakery Management System");

            return new EmailMessageModel
            {
                Subject = subject,
                Body = sb.ToString()
            };
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//