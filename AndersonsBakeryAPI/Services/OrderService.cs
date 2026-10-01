using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using LogiSyn.Model;
using LogiSyn.Interface;

namespace LogiSyn.Services
{
    public class OrderService : IOrderService
    {
        private readonly ITempRecipeService _recipeRepo;

        // Shared static list ensures persistence across all views in memory
        private static readonly List<OrderScaled> _orders = new();

        public OrderService() : this(new TempRecipeService()) { }

        public OrderService(ITempRecipeService recipeRepo)
        {
            _recipeRepo = recipeRepo ?? throw new ArgumentNullException(nameof(recipeRepo));
        }

        // ==========================================
        // 1. PDF Reading & Order Scaling Pipeline
        // ==========================================

        public OrderScaled ReadAndScaleOrder(string pdfPath, string fallbackOrderId = "#001")
        {
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("PDF file not found.", pdfPath);

            var extractedLines = ReadPdfLines(pdfPath).ToList();

            // Extract metadata from header
            string parsedOrderId = ExtractField(extractedLines, @"Order\s*(?:Number|#)?[:\s]*([#A-Za-z0-9\-]+)");
            string parsedCustomer = ExtractCustomer(extractedLines);
            DateTime? parsedDate = ExtractOrderDate(extractedLines);

            // Determine candidate order ID
            string rawOrderId = !string.IsNullOrWhiteSpace(parsedOrderId) ? parsedOrderId : fallbackOrderId;

            // Automatically iterate ID if an order with that ID already exists
            string uniqueOrderId = EnsureUniqueOrderId(rawOrderId);

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
                    // Lookahead: Combine current product line with the following lines to capture "pkts 250"
                    string context = line;
                    for (int offset = 1; offset <= 2 && (i + offset) < extractedLines.Count; offset++)
                    {
                        if (!IsDividerLine(extractedLines[i + offset]))
                        {
                            context += " " + extractedLines[i + offset];
                        }
                    }

                    int quantity = ExtractQuantity(context);

                    var item = new ProductionItem
                    {
                        ProductName = $"{quantity} {recipe.DisplayName}",
                        ProductionLine = recipe.ProductionArea,
                        packaging = new Packaging
                        {
                            Pans = recipe.UnitsPerPan > 0
                                ? (int)Math.Ceiling((double)quantity / recipe.UnitsPerPan)
                                : 2,
                            Trollies = recipe.PansPerTrolley > 0
                                ? (int)Math.Ceiling((double)quantity / (recipe.UnitsPerPan * recipe.PansPerTrolley))
                                : 1,
                            PansUsed = 0,
                            TrolliesUsed = 0
                        }
                    };

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

        // ==========================================
        // 2. ID Deduplication Logic
        // ==========================================

        public string EnsureUniqueOrderId(string baseOrderId)
        {
            if (string.IsNullOrWhiteSpace(baseOrderId))
                baseOrderId = "#001";

            string candidateId = baseOrderId;

            // Pure numeric format like "#002" or "002"
            var numMatch = Regex.Match(baseOrderId, @"^(#?)(\d+)$");
            if (numMatch.Success)
            {
                string prefix = numMatch.Groups[1].Value;
                string digits = numMatch.Groups[2].Value;
                int num = int.Parse(digits);
                int padLength = digits.Length;

                while (_orders.Any(o => o.OrderId.Equals(candidateId, StringComparison.OrdinalIgnoreCase)))
                {
                    num++;
                    candidateId = $"{prefix}{num.ToString().PadLeft(padLength, '0')}";
                }

                return candidateId;
            }

            // Alphanumeric format -> appends "-1", "-2"
            int suffix = 1;
            while (_orders.Any(o => o.OrderId.Equals(candidateId, StringComparison.OrdinalIgnoreCase)))
            {
                candidateId = $"{baseOrderId}-{suffix}";
                suffix++;
            }

            return candidateId;
        }

        // ==========================================
        // 3. Raw Material Calculations
        // ==========================================

        public void RecalRawMaterials(OrderScaled order)
        {
            if (order == null) return;

            var aggregates = new Dictionary<string, (double Amount, string Unit)>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in order.productionItems)
            {
                foreach (var ing in item.ReqIngredients)
                {
                    double totalRequired = ing.IngredientAmount + ing.AdditionsAmount;

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

            order.RawMaterials = aggregates;
        }

        // ==========================================
        // 4. Persistence & History Management
        // ==========================================

        public IEnumerable<OrderScaled> GetOrders() => _orders.ToList();

        public IEnumerable<OrderScaled> GetHistory() =>
            _orders.Where(o => o.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)).ToList();

        public OrderScaled? GetOrderById(string orderId) =>
            _orders.FirstOrDefault(o => o.OrderId.Equals(orderId, StringComparison.OrdinalIgnoreCase));

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

        public void CompleteOrder(string orderId, Action<OrderScaled> recordOrderData)
        {
            var order = GetOrderById(orderId);
            if (order == null) return;

            recordOrderData?.Invoke(order);
            order.Status = "Completed";
        }

        // ==========================================
        // 5. Extraction Helpers
        // ==========================================

        private IEnumerable<string> ReadPdfLines(string pdfPath)
        {
            var lines = new List<string>();

            using var doc = PdfDocument.Open(pdfPath);
            foreach (var page in doc.GetPages())
            {
                var text = page.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;

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

        private string ExtractCustomer(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                // Stops capturing at subsequent labels ("Order", "Date", "Delivery", etc.)
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

        private DateTime? ExtractOrderDate(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith("Order Date", StringComparison.OrdinalIgnoreCase))
                {
                    var clean = line.Replace("Order Date:", "").Replace("Order Date", "").Trim();
                    if (DateTime.TryParse(clean, out var dt)) return dt;
                }

                var match = Regex.Match(line, @"\b\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{4}\b", RegexOptions.IgnoreCase);
                if (match.Success && DateTime.TryParse(match.Value, out var parsed))
                {
                    return parsed;
                }

                var numDateMatch = Regex.Match(line, @"\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b");
                if (numDateMatch.Success && DateTime.TryParse(numDateMatch.Value, out var dtNum))
                {
                    return dtNum;
                }
            }
            return null;
        }

        private int ExtractQuantity(string text)
        {
            // 1. Matches "pkts 250", "pkts | 110", or "pkts: 45"
            var pktMatch = Regex.Match(text, @"(?:pkts\s*\|?\s*|Quantity\s*\|?\s*)(\d{1,4})\b", RegexOptions.IgnoreCase);
            if (pktMatch.Success && int.TryParse(pktMatch.Groups[1].Value, out int pktQty))
            {
                return pktQty;
            }

            // 2. Grabs typical order quantities (2 to 4 digits), skipping small single digits
            var allNumbers = Regex.Matches(text, @"\b(\d{2,4})\b");
            if (allNumbers.Count > 0)
            {
                if (int.TryParse(allNumbers[allNumbers.Count - 1].Value, out int fallbackQty))
                    return fallbackQty;
            }

            return 100; // Default fallback to prevent multiplying by 0
        }

        private bool IsDividerLine(string line)
        {
            return line.Trim().All(c => c == '=' || c == '-' || c == '_' || c == '*' || char.IsWhiteSpace(c));
        }
    }
}