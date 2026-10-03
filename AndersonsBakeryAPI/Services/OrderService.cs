using LogiSyn.Interface;
using LogiSyn.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AndersonsBakeryAPI.Services
{
    public class OrderService : IOrderService
    {
        // Initialise lists and repositories for order processing
        private readonly ITempRecipeService _recipeRepo;

        private static readonly List<OrderScaled> _orders = new();

        public OrderService() : this(new TempRecipeService()) { }

        // TODO: Refactor when merged with the main project to use actual recipe repository instead of the temporary one
        public OrderService(ITempRecipeService recipeRepo)
        {
            _recipeRepo = recipeRepo ?? throw new ArgumentNullException(nameof(recipeRepo));
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

            // Loop through lines to match products from recipes
            for (int i = 0; i < extractedLines.Count; i++)
            {
                string line = extractedLines[i];

                if (IsDividerLine(line)) continue;

                var recipe = _recipeRepo.GetAllRecipes()
                    .FirstOrDefault(r => line.IndexOf(r.MatchPattern, StringComparison.OrdinalIgnoreCase) >= 0);

                if (recipe != null)
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

                    // Create a production item based on the recipe and calculated quantity
                    var item = new ProductionItem
                    {
                        ProductName = $"{quantity} {recipe.DisplayName}",
                        ProductionLine = recipe.ProductionArea,
                        packaging = new Packaging
                        {
                            Pans = recipe.UnitsPerPan > 0
                                ? (int)Math.Ceiling((double)quantity / recipe.UnitsPerPan)
                                : 2,
                            Trolleys = recipe.PansPerTrolley > 0
                                ? (int)Math.Ceiling((double)quantity / (recipe.UnitsPerPan * recipe.PansPerTrolley))
                                : 1,
                            PansUsed = 0,
                            TrolleysUsed = 0
                        }
                    };

                    // Scale and add the required ingredients for this production item
                    foreach (var ing in recipe.Ingredients)
                    {
                        item.ReqIngredients.Add(new Ingredients
                        {
                            IngredientName = ing.Name,
                            IngredientAmount = Math.Round(ing.AmountPerUnit * quantity, 1),
                            AdditionsAmount = Math.Round(ing.AdditionalRatio * quantity, 1),
                            MeasuredIngredient = ing.Unit,
                            AmountUsed = Math.Round(ing.DefaultUsedRatio * quantity, 1)
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

            // Check if the order ID is purely numeric (with optional '#' prefix)
            var numMatch = Regex.Match(baseOrderId, @"^(#?)(\d+)$");
            if (numMatch.Success)
            {
                string prefix = numMatch.Groups[1].Value;
                string digits = numMatch.Groups[2].Value;
                int num = int.Parse(digits);
                int padLength = digits.Length;

                // Increment the numeric part until a unique order ID is found
                while (_orders.Any(o => o.OrderId.Equals(candidateId, StringComparison.OrdinalIgnoreCase)))
                {
                    num++;
                    candidateId = $"{prefix}{num.ToString().PadLeft(padLength, '0')}";
                }

                return candidateId;
            }

            // If the order ID is not purely numeric, Add a suffix to ensure a unique order ID
            int suffix = 1;
            while (_orders.Any(o => o.OrderId.Equals(candidateId, StringComparison.OrdinalIgnoreCase)))
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

        // Methods to manage orders in memory, including retrieval, saving, and completion
        public IEnumerable<OrderScaled> GetOrders() => _orders.ToList();

        // Retrieves only completed orders for history purposes, To be used in the history page as well
        public IEnumerable<OrderScaled> GetHistory() =>
            _orders.Where(o => o.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)).ToList();

        // Retrieves a specific order by its ID, ignoring case sensitivity
        public OrderScaled? GetOrderById(string orderId) =>
            _orders.FirstOrDefault(o => o.OrderId.Equals(orderId, StringComparison.OrdinalIgnoreCase));

        // Saves or updates an order in the in-memory list, replacing any existing order with the same ID
        public void SaveOrder(OrderScaled order)
        {
            if (order == null) return;

            var existing = GetOrderById(order.OrderId);
            if (existing != null)
            {
                _orders.Remove(existing);
            }
            _orders.Add(order);
        }

        // ------------------------------------------------------------------------------------------------//

        // Marks an order as completed and allows for additional processing via a callback action
        public void CompleteOrder(string orderId, Action<OrderScaled> recordOrderData)
        {
            var order = GetOrderById(orderId);
            if (order == null) return;

            recordOrderData?.Invoke(order);
            order.Status = "Completed";
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