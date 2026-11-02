using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SharedLibrary.Interface;
using SharedLibrary.Model;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AndersonsBakeryAPI.Services
{
    // Ingestion service responsible for reading raw PDF text and extracting structured order headers and product lines
    public class PdfOrderParserService
    {
        // Parses the PDF, extracts order details, and scales the ingredients based on recipes
        public OrderScaled ReadAndScaleOrder(
            string pdfPath,
            string fallbackOrderId,
            IProductService productService,
            ITempRecipeService? recipeService,
            Func<string, string> ensureUniqueOrderId,
            Action<OrderScaled> recalRawMaterials)
        {
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("PDF file not found.", pdfPath);

            var extractedLines = ReadPdfLines(pdfPath).ToList();

            // Extract metadata from header
            string parsedOrderId = ExtractField(extractedLines, @"Order\s*(?:Number|No\.?|#)\s*[:\-\s]*([#A-Za-z0-9\-]+)");
            string parsedCustomer = ExtractCustomer(extractedLines);
            DateTime? parsedDate = ExtractOrderDate(extractedLines);

            string rawOrderId = !string.IsNullOrWhiteSpace(parsedOrderId) ? parsedOrderId : fallbackOrderId;
            string uniqueOrderId = ensureUniqueOrderId(rawOrderId);

            var order = new OrderScaled
            {
                OrderId = uniqueOrderId,
                Customer = !string.IsNullOrWhiteSpace(parsedCustomer) ? parsedCustomer : "Spar",
                OrderDate = parsedDate ?? DateTime.Now,
                Status = "Pending",
                productionItems = new List<ProductionItem>()
            };

            var availableProducts = productService.GetAllProducts();

            // Track which products have already been added to avoid duplicates
            var addedProducts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < extractedLines.Count; i++)
            {
                string line = extractedLines[i];

                if (IsDividerLine(line)) continue;

                // Find ALL products mentioned in this line, not just the first one
                // Sort by longest name first so "Hotdog Rolls" matches before "Bread" in overlapping cases
                var matchedProducts = availableProducts
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProductName) &&
                                line.IndexOf(p.ProductName, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderByDescending(p => p.ProductName!.Length)
                    .ToList();

                foreach (var product in matchedProducts)
                {
                    string cleanProdName = product.ProductName ?? string.Empty;
                    var qtyMatch = Regex.Match(cleanProdName, @"^(\d+)\s+(.+)$");
                    if (qtyMatch.Success)
                    {
                        cleanProdName = qtyMatch.Groups[2].Value.Trim();
                    }

                    // Skip if this product was already added from a previous line
                    if (addedProducts.Contains(cleanProdName)) continue;

                    // Build a context window: extract the segment of text near this product name
                    int nameIndex = line.IndexOf(product.ProductName!, StringComparison.OrdinalIgnoreCase);
                    string context;
                    if (matchedProducts.Count > 1)
                    {
                        // Multiple products on this line — extract a local segment around this product
                        context = ExtractProductSegment(line, product.ProductName!, availableProducts);
                    }
                    else
                    {
                        // Single product on this line — use the whole line plus lookahead
                        context = line;
                        for (int offset = 1; offset <= 2 && (i + offset) < extractedLines.Count; offset++)
                        {
                            if (!IsDividerLine(extractedLines[i + offset]))
                            {
                                context += " " + extractedLines[i + offset];
                            }
                        }
                    }

                    int quantity = ExtractQuantity(context);

                    var recipe = recipeService?.FindRecipeByProductName(cleanProdName)
                                 ?? recipeService?.FindRecipeByProductName(product.ProductName ?? string.Empty);

                    int unitsPerPan = recipe != null && recipe.UnitsPerPan > 0 ? recipe.UnitsPerPan : 50;
                    int pansPerTrolley = recipe != null && recipe.PansPerTrolley > 0 ? recipe.PansPerTrolley : 2;

                    int pans = (int)Math.Ceiling((double)quantity / unitsPerPan);
                    int trolleys = (int)Math.Ceiling((double)pans / pansPerTrolley);

                    string prodArea = !string.IsNullOrWhiteSpace(recipe?.ProductionArea)
                        ? recipe.ProductionArea
                        : (!string.IsNullOrWhiteSpace(product.StorageLocation) ? product.StorageLocation : "Production 1");

                    var item = new ProductionItem
                    {
                        ProductName = cleanProdName,
                        Amount = quantity,
                        ProductionLine = prodArea,
                        packaging = new Packaging
                        {
                            Pans = pans,
                            Trolleys = trolleys,
                            PansUsed = 0,
                            TrolleysUsed = 0
                        }
                    };

                    if (recipe != null && recipe.Ingredients != null && recipe.Ingredients.Count > 0)
                    {
                        foreach (var ing in recipe.Ingredients)
                        {
                            item.ReqIngredients.Add(new Ingredients
                            {
                                IngredientName = ing.Name,
                                IngredientAmount = Math.Round(ing.AmountPerUnit * quantity, 2),
                                AdditionsAmount = Math.Round(ing.AdditionalRatio * quantity, 2),
                                MeasuredIngredient = ing.Unit ?? "bags",
                                AmountUsed = 0
                            });
                        }
                    }
                    else if (product.Ingredients != null && product.Ingredients.Count > 0)
                    {
                        foreach (var ing in product.Ingredients)
                        {
                            double baseQty = (double)ing.Quantity;
                            item.ReqIngredients.Add(new Ingredients
                            {
                                IngredientName = ing.IngredientName,
                                IngredientAmount = Math.Round(baseQty * quantity, 2),
                                AdditionsAmount = Math.Round(baseQty * 0.1 * quantity, 2),
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
                            IngredientAmount = Math.Round(0.04 * quantity, 2),
                            AdditionsAmount = Math.Round(0.005 * quantity, 2),
                            MeasuredIngredient = "kg",
                            AmountUsed = 0
                        });
                        item.ReqIngredients.Add(new Ingredients
                        {
                            IngredientName = "Yeast",
                            IngredientAmount = Math.Round(0.01 * quantity, 2),
                            AdditionsAmount = Math.Round(0.001 * quantity, 2),
                            MeasuredIngredient = "kg",
                            AmountUsed = 0
                        });
                    }

                    order.productionItems.Add(item);
                    addedProducts.Add(cleanProdName);
                }
            }

            recalRawMaterials(order);
            return order;
        }

        // Extracts the text segment belonging to a specific product when multiple products share a line.
        // Finds the text between this product's name and the next product's name (or end of line).
        private string ExtractProductSegment(string line, string productName, List<Product> allProducts)
        {
            int start = line.IndexOf(productName, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return line;

            // Find the nearest subsequent product name after this one
            int segmentEnd = line.Length;
            foreach (var other in allProducts)
            {
                if (string.IsNullOrWhiteSpace(other.ProductName)) continue;
                if (string.Equals(other.ProductName, productName, StringComparison.OrdinalIgnoreCase)) continue;

                int otherPos = line.IndexOf(other.ProductName, start + productName.Length, StringComparison.OrdinalIgnoreCase);
                if (otherPos >= 0 && otherPos < segmentEnd)
                {
                    segmentEnd = otherPos;
                }
            }

            return line.Substring(start, segmentEnd - start);
        }

        // Helper method to read all lines of text from a PDF file using PdfPig.
        // Falls back to word-level Y-coordinate grouping when the raw text has no newlines.
        public IEnumerable<string> ReadPdfLines(string pdfPath)
        {
            var lines = new List<string>();

            using var doc = PdfDocument.Open(pdfPath);
            foreach (var page in doc.GetPages())
            {
                // First try: use PdfPig's raw text with standard newline splitting
                var text = page.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;

                var pageLines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // If the split produced real multi-line output, use it
                if (pageLines.Length > 1 || (pageLines.Length == 1 && pageLines[0].Length < 200))
                {
                    foreach (var l in pageLines)
                    {
                        var trimmed = l.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            lines.Add(trimmed);
                        }
                    }
                }
                else
                {
                    // Fallback: PdfPig returned everything as one giant string.
                    // Reconstruct lines by grouping words by their Y-coordinate.
                    var words = page.GetWords().ToList();
                    if (words.Count == 0)
                    {
                        // No words extracted — add the raw text as a single line
                        lines.Add(text.Trim());
                        continue;
                    }

                    // Group words by their vertical position (bottom of bounding box).
                    // Words within 3 points of each other vertically are on the same line.
                    const double yTolerance = 3.0;
                    var sortedWords = words.OrderByDescending(w => w.BoundingBox.Bottom)
                                           .ThenBy(w => w.BoundingBox.Left)
                                           .ToList();

                    var currentLineWords = new List<Word> { sortedWords[0] };
                    double currentY = sortedWords[0].BoundingBox.Bottom;

                    for (int w = 1; w < sortedWords.Count; w++)
                    {
                        double wordY = sortedWords[w].BoundingBox.Bottom;
                        if (Math.Abs(wordY - currentY) <= yTolerance)
                        {
                            currentLineWords.Add(sortedWords[w]);
                        }
                        else
                        {
                            // Emit the completed line
                            var lineText = string.Join(" ", currentLineWords.OrderBy(x => x.BoundingBox.Left).Select(x => x.Text));
                            if (!string.IsNullOrWhiteSpace(lineText))
                                lines.Add(lineText.Trim());

                            currentLineWords = new List<Word> { sortedWords[w] };
                            currentY = wordY;
                        }
                    }

                    // Emit the last line
                    if (currentLineWords.Count > 0)
                    {
                        var lineText = string.Join(" ", currentLineWords.OrderBy(x => x.BoundingBox.Left).Select(x => x.Text));
                        if (!string.IsNullOrWhiteSpace(lineText))
                            lines.Add(lineText.Trim());
                    }
                }
            }

            return lines;
        }

        // Helper method to extract the customer name from the PDF lines, stopping at subsequent labels
        public string ExtractCustomer(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
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

        // Extracts a regex group from lines
        public string ExtractField(IEnumerable<string> lines, string regexPattern)
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

        // Helper method to extract the order date from the PDF lines, supporting multiple formats
        public DateTime? ExtractOrderDate(IEnumerable<string> lines)
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

        // Helper method to extract quantity from a line of text, ignoring dates, years, and prices
        public int ExtractQuantity(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 100;

            string cleaned = Regex.Replace(text, @"\b\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4}\b", " ");
            cleaned = Regex.Replace(cleaned, @"\b\d{4}[\/\-\.]\d{1,2}[\/\-\.]\d{1,2}\b", " ");
            cleaned = Regex.Replace(cleaned, @"\b\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{4}\b", " ", RegexOptions.IgnoreCase);

            cleaned = Regex.Replace(cleaned, @"(?:[R\$€£]|@)\s*\d+(?:\.\d+)?", " ");
            cleaned = Regex.Replace(cleaned, @"\b(?:Price|UnitPrice|Cost|Total)\s*[:=]?\s*\d+(?:\.\d+)?", " ", RegexOptions.IgnoreCase);

            var explicitMatch = Regex.Match(cleaned, @"(?:pkts\s*[:\|\-]?\s*|Quantity\s*[:\|\-]?\s*|Qty\s*[:\|\-]?\s*|Units?\s*[:\|\-]?\s*)(\d{1,4})\b", RegexOptions.IgnoreCase);
            if (explicitMatch.Success && int.TryParse(explicitMatch.Groups[1].Value, out int explicitQty) && explicitQty > 0)
            {
                if (explicitQty < 1990 || explicitQty > 2099)
                    return explicitQty;
            }

            var allNumbers = Regex.Matches(cleaned, @"\b(\d{1,4})\b");
            var validNumbers = new List<int>();
            foreach (Match m in allNumbers)
            {
                if (int.TryParse(m.Groups[1].Value, out int num))
                {
                    if (num >= 1990 && num <= 2099) continue;
                    if (num > 0) validNumbers.Add(num);
                }
            }

            if (validNumbers.Count > 0)
            {
                return validNumbers[0];
            }

            return 100;
        }

        // Helper method to identify divider lines in the PDF text
        public bool IsDividerLine(string line)
        {
            return line.Trim().All(c => c == '=' || c == '-' || c == '_' || c == '*' || char.IsWhiteSpace(c));
        }
    }
}
/*********************************************EOF*******************************************/
