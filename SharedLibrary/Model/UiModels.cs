// Adriaan
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using MongoDB.Bson;

using MongoDB.Bson.Serialization.Attributes;

#if WINDOWS
using System.Windows.Media;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
#else
using ImageSource = System.Object;
using Brush = System.Object;
#endif

namespace SharedLibrary.Model
{
    // =====================================================================
    //  Front-end models + sample data.
    //  Everything in SampleData is placeholder text copied from the Figma
    //  screens - swap these calls for your real data / services later.
    // =====================================================================

    public enum AppRole { Admin, Manager, User }

    /// <summary>One item in the left sidebar.</summary>
    public class NavItem : INotifyPropertyChanged
    {
        private bool _isActive;

        public string Key { get; set; }
        public string Label { get; set; }
        public ImageSource Icon { get; set; }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsActive"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

#if WINDOWS
    public static class Palette
    {
        public static Brush From(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }

        public static readonly Brush ListPending = From("#BDD38478");
        public static readonly Brush ListComplete = From("#BD398158");
        public static readonly Brush DashPending = From("#C6AEA5");
        public static readonly Brush DashComplete = From("#A5C6AC");
        public static readonly Brush UserPending = From("#B22727");
        public static readonly Brush UserComplete = From("#135826");
        public static readonly Brush Black = From("#000000");
    }
#else
    public static class Palette
    {
        public static Brush From(string hex) => null!;
        public static readonly Brush ListPending = null!;
        public static readonly Brush ListComplete = null!;
        public static readonly Brush DashPending = null!;
        public static readonly Brush DashComplete = null!;
        public static readonly Brush UserPending = null!;
        public static readonly Brush UserComplete = null!;
        public static readonly Brush Black = null!;
    }
#endif

    // Adriaan - Orders section
    public class OrderRow
    {
        public string Number { get; set; }
        public string Customer { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }              // "Pending" or "Complete"

        public bool IsComplete { get { return Status == "Complete" || Status == "Completed"; } }
        public string DateText { get { return Date.ToString("dd/MM/yy", CultureInfo.InvariantCulture); } }

        public Brush ListStatusBrush { get { return IsComplete ? Palette.ListComplete : Palette.ListPending; } }
        public Brush DashStatusBrush { get { return IsComplete ? Palette.DashComplete : Palette.DashPending; } }
        public string UserStatusText { get { return IsComplete ? "Completed" : "Pending"; } }
        public Brush UserStatusBrush { get { return IsComplete ? Palette.UserComplete : Palette.UserPending; } }

        /// <summary>
        /// Factory method to create an OrderRow from an OrderScaled (API response)
        /// </summary>
        public static OrderRow FromOrderScaled(OrderScaled order)
        {
            return new OrderRow
            {
                Number = order.OrderId,
                Customer = order.Customer,
                Date = order.OrderDate,
                Status = order.Status == "Completed" ? "Complete" : order.Status
            };
        }
    }

    public class ProductRow
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? Price { get; set; }
        public string? SellBy { get; set; }
        public string? BestBefore { get; set; }
        public string? Storage { get; set; }
    }

    public class UserRow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonIgnore]
        public string SqlId { get; set; } = string.Empty;

        [BsonElement("Username")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Password")]
        [JsonIgnore]
        public string Password { get; set; } = string.Empty;

        [BsonElement("Role")]
        public string Role { get; set; } = string.Empty;

        [BsonElement("DateAdded")]
        public string DateAdded { get; set; } = string.Empty;
    }

    // ----- summary paper (Order Sheets / Order Breakdown) -----
    public class SummaryLine
    {
        public string Product { get; set; }
        public string Production { get; set; }
        public string Ingredients { get; set; }
        public string Packaging { get; set; }
    }

    // ----- production sheet paper -----
    public class SheetIngredient
    {
        public string Name { get; set; }
        public string Amount { get; set; }
        public string Additional { get; set; }
        public string Used { get; set; }
        public Brush UsedBrush { get; set; }
    }

    public class SheetPackaging
    {
        public string Name { get; set; }
        public string Amount { get; set; }
        public string Used { get; set; }
        public Brush UsedBrush { get; set; }
    }

    public class SheetProduct
    {
        public string Name { get; set; }
        public string Amount { get; set; }
        public string Production { get; set; }
        public List<SheetIngredient> Ingredients { get; set; }
        public List<SheetPackaging> Packaging { get; set; }
        public string Notes { get; set; }
    }

    // Adriaan - Order Sheet & Summary section
    public class SheetData
    {
        public string Title { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public List<SheetProduct> Products { get; set; } = new();

        /// <summary>
        /// Converts an OrderScaled domain model to SheetData for display in SheetPaper.
        /// </summary>
        public static SheetData FromOrderScaled(OrderScaled order)
        {
            if (order == null) return SampleData.Sheet(false);

            bool isCompleted = string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(order.Status, "Complete", StringComparison.OrdinalIgnoreCase);

            Brush good = Palette.UserComplete;
            Brush bad = Palette.UserPending;
            Brush normalBrush = isCompleted ? good : Palette.Black;

            var sheetProducts = new List<SheetProduct>();
            var prodItems = order.ProductionItems ?? order.productionItems ?? new List<ProductionItem>();

            foreach (var pi in prodItems)
            {
                // Extract clean product name and quantity
                string cleanName = pi.ProductName ?? string.Empty;
                string amountStr = pi.Amount > 0 ? pi.Amount.ToString() : "100";

                var match = Regex.Match(cleanName, @"^(\d+)\s+(.+)$");
                if (match.Success)
                {
                    amountStr = match.Groups[1].Value;
                    cleanName = match.Groups[2].Value;
                }

                // Build ingredients list
                var sheetIngredients = new List<SheetIngredient>();
                foreach (var ing in pi.ReqIngredients ?? new List<Ingredients>())
                {
                    double baseAmt = ing.IngredientAmount;
                    double additionalAmt = Math.Round(ing.IngredientAmount + ing.AdditionsAmount, 2);
                    string unit = string.IsNullOrWhiteSpace(ing.MeasuredIngredient) ? "kg" : ing.MeasuredIngredient;

                    string usedText = "";
                    Brush usedBrush = normalBrush;

                    if (ing.AmountUsed > 0)
                    {
                        usedText = isCompleted ? $"{ing.AmountUsed} {unit}".Trim() : ing.AmountUsed.ToString(CultureInfo.InvariantCulture);
                        usedBrush = ing.AmountUsed > additionalAmt ? bad : good;
                    }
                    else if (isCompleted)
                    {
                        usedText = $"{additionalAmt} {unit}".Trim();
                        usedBrush = good;
                    }

                    sheetIngredients.Add(new SheetIngredient
                    {
                        Name = ing.IngredientName,
                        Amount = $"{baseAmt} {unit}".Trim(),
                        Additional = $"{additionalAmt} {unit}".Trim(),
                        Used = usedText,
                        UsedBrush = usedBrush
                    });
                }

                // Build packaging list
                var sheetPackaging = new List<SheetPackaging>();
                var pack = pi.Packaging ?? new Packaging();

                // Pans
                string pansUsedText = "";
                Brush pansBrush = normalBrush;
                if (pack.PansUsed > 0)
                {
                    pansUsedText = pack.PansUsed.ToString(CultureInfo.InvariantCulture);
                    pansBrush = pack.PansUsed > pack.Pans ? bad : good;
                }
                else if (isCompleted)
                {
                    pansUsedText = pack.Pans.ToString(CultureInfo.InvariantCulture);
                    pansBrush = good;
                }
                sheetPackaging.Add(new SheetPackaging
                {
                    Name = "Pans",
                    Amount = pack.Pans.ToString(CultureInfo.InvariantCulture),
                    Used = pansUsedText,
                    UsedBrush = pansBrush
                });

                // Trolleys
                string trolleysUsedText = "";
                Brush trolleysBrush = normalBrush;
                if (pack.TrolleysUsed > 0)
                {
                    trolleysUsedText = pack.TrolleysUsed.ToString(CultureInfo.InvariantCulture);
                    trolleysBrush = pack.TrolleysUsed > pack.Trolleys ? bad : good;
                }
                else if (isCompleted)
                {
                    trolleysUsedText = pack.Trolleys.ToString(CultureInfo.InvariantCulture);
                    trolleysBrush = good;
                }
                sheetPackaging.Add(new SheetPackaging
                {
                    Name = "Trolleys",
                    Amount = pack.Trolleys.ToString(CultureInfo.InvariantCulture),
                    Used = trolleysUsedText,
                    UsedBrush = trolleysBrush
                });

                sheetProducts.Add(new SheetProduct
                {
                    Name = cleanName,
                    Amount = amountStr,
                    Production = string.IsNullOrWhiteSpace(pi.ProductionLine) ? "Production 1" : pi.ProductionLine,
                    Ingredients = sheetIngredients,
                    Packaging = sheetPackaging,
                    Notes = pi.Notes ?? string.Empty
                });
            }

            return new SheetData
            {
                Title = $"{order.Customer} Order {order.OrderId}".Trim(),
                DateText = order.OrderDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture),
                IsCompleted = isCompleted,
                Products = sheetProducts
            };
        }

        /// <summary>
        /// Applies edits from SheetData (worker inputs: Used ingredients, Used packaging, Notes) back to the OrderScaled domain model.
        /// </summary>
        public static void ApplyToOrderScaled(SheetData sheet, OrderScaled order, bool isCompleting = false)
        {
            if (sheet == null || order == null) return;

            if (sheet.Products == null) return;

            var orderItems = order.ProductionItems ?? order.productionItems ?? new List<ProductionItem>();

            for (int i = 0; i < sheet.Products.Count; i++)
            {
                var sp = sheet.Products[i];
                ProductionItem? pi = null;

                if (i < orderItems.Count)
                {
                    pi = orderItems[i];
                }
                else
                {
                    pi = orderItems.FirstOrDefault(p =>
                        !string.IsNullOrWhiteSpace(sp.Name) &&
                        p.ProductName.IndexOf(sp.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (pi == null) continue;

                // Save worker notes
                pi.Notes = sp.Notes ?? string.Empty;

                // Map ingredients
                if (sp.Ingredients != null && pi.ReqIngredients != null)
                {
                    foreach (var si in sp.Ingredients)
                    {
                        var ing = pi.ReqIngredients.FirstOrDefault(r =>
                            string.Equals(r.IngredientName, si.Name, StringComparison.OrdinalIgnoreCase));
                        if (ing != null)
                        {
                            var parsedUsed = ParseDouble(si.Used);
                            if (parsedUsed.HasValue)
                            {
                                ing.AmountUsed = parsedUsed.Value;
                            }
                            else if (isCompleting && ing.AmountUsed <= 0)
                            {
                                ing.AmountUsed = Math.Round(ing.IngredientAmount + ing.AdditionsAmount, 2);
                            }
                        }
                    }
                }

                // Map packaging
                if (sp.Packaging != null)
                {
                    pi.Packaging ??= new Packaging();

                    var pansItem = sp.Packaging.FirstOrDefault(p => string.Equals(p.Name, "Pans", StringComparison.OrdinalIgnoreCase));
                    if (pansItem != null)
                    {
                        var parsed = ParseDouble(pansItem.Used);
                        if (parsed.HasValue)
                        {
                            pi.Packaging.PansUsed = parsed.Value;
                        }
                        else if (isCompleting && pi.Packaging.PansUsed <= 0)
                        {
                            pi.Packaging.PansUsed = pi.Packaging.Pans;
                        }
                    }

                    var trolleysItem = sp.Packaging.FirstOrDefault(p => string.Equals(p.Name, "Trolleys", StringComparison.OrdinalIgnoreCase));
                    if (trolleysItem != null)
                    {
                        var parsed = ParseDouble(trolleysItem.Used);
                        if (parsed.HasValue)
                        {
                            pi.Packaging.TrolleysUsed = parsed.Value;
                        }
                        else if (isCompleting && pi.Packaging.TrolleysUsed <= 0)
                        {
                            pi.Packaging.TrolleysUsed = pi.Packaging.Trolleys;
                        }
                    }
                }
            }
        }

        private static double? ParseDouble(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string normalized = text.Trim().Replace(',', '.');
            var match = Regex.Match(normalized, @"(\d+(?:\.\d+)?)");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
            return null;
        }

        private static int? ParseInt(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Regex.Match(text, @"\b(\d+)\b");
            if (match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out int val))
            {
                return val;
            }
            return null;
        }
    }

    public class SummaryData
    {
        public string Title { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public List<SummaryLine> Lines { get; set; } = new();

        /// <summary>
        /// Converts an OrderScaled domain model to SummaryData for display in SummaryPaper.
        /// </summary>
        public static SummaryData FromOrderScaled(OrderScaled order)
        {
            if (order == null) return SampleData.Summary(true);

            bool completed = string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(order.Status, "Complete", StringComparison.OrdinalIgnoreCase);

            string title = $"{order.Customer} Order {order.OrderId}".Trim();
            string dateText = order.OrderDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

            var lines = new List<SummaryLine>();
            var items = order.ProductionItems ?? order.productionItems ?? new List<ProductionItem>();

            foreach (var item in items)
            {
                string prodName = item.ProductName ?? string.Empty;
                if (!prodName.StartsWith(item.Amount.ToString()) && item.Amount > 0)
                {
                    prodName = $"{item.Amount} {item.ProductName}".Trim();
                }

                var ingStrings = (item.ReqIngredients ?? new List<Ingredients>()).Select(i =>
                {
                    double displayAmt = completed && i.AmountUsed > 0
                        ? i.AmountUsed
                        : Math.Round(i.IngredientAmount + i.AdditionsAmount, 2);
                    string unit = string.IsNullOrWhiteSpace(i.MeasuredIngredient) ? "kg" : i.MeasuredIngredient;
                    return $"{i.IngredientName}: {displayAmt} {unit}".Trim();
                });

                double pans = completed && item.Packaging?.PansUsed > 0 ? item.Packaging.PansUsed : (item.Packaging?.Pans ?? 0);
                double trolleys = completed && item.Packaging?.TrolleysUsed > 0 ? item.Packaging.TrolleysUsed : (item.Packaging?.Trolleys ?? 0);

                lines.Add(new SummaryLine
                {
                    Product = prodName,
                    Production = string.IsNullOrWhiteSpace(item.ProductionLine) ? "Production 1" : item.ProductionLine,
                    Ingredients = string.Join(",   ", ingStrings),
                    Packaging = $"Pans: {pans},   Trolleys: {trolleys}"
                });
            }

            return new SummaryData
            {
                Title = title,
                DateText = dateText,
                IsCompleted = completed,
                Lines = lines
            };
        }
    }

    public class RawMaterialData
    {
        public string Title { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public List<string> Totals { get; set; } = new();

        public static RawMaterialData FromOrderScaled(OrderScaled order)
        {
            if (order == null) return SampleData.RawMaterials();

            return new RawMaterialData
            {
                Title = $"{order.Customer} Order {order.OrderId}".Trim(),
                DateText = order.OrderDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture),
                Totals = order.RawMaterials?.Select(kvp => $"{kvp.Key}: {kvp.Value.Amount} {kvp.Value.Unit}".Trim()).ToList()
                         ?? new List<string>()
            };
        }
    }

    // ----- product pop-up -----
    public class IngredientLine
    {
        public string Name { get; set; }
        public string Quantity { get; set; }
    }

    public class ProductDetail
    {
        public string Name { get; set; }
        public string DateAdded { get; set; }
        public List<IngredientLine> Ingredients { get; set; }
        public string Method { get; set; }
        public string Storage { get; set; }
    }

    // =====================================================================
    public static class SampleData
    {
        public static string Today()
        {
            return DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        // Adriaan - Sample Order Data
        // Admin "Orders" list and the dashboard's recent orders
        public static List<OrderRow> Orders()
        {
            return new List<OrderRow>
            {
                new OrderRow { Number = "#001", Customer = "Checkers", Date = new DateTime(2026, 5, 9), Status = "Pending" },
                new OrderRow { Number = "#002", Customer = "Spar",     Date = new DateTime(2026, 5, 9), Status = "Complete" }
            };
        }

        public static List<OrderScaled> SampleOrdersScaled()
        {
            return new List<OrderScaled>
            {
                new OrderScaled
                {
                    OrderId = "#001",
                    Customer = "Checkers",
                    OrderDate = DateTime.Now.Date.AddDays(-1),
                    Status = "Pending",
                    ProductionItems = new List<ProductionItem>
                    {
                        new ProductionItem
                        {
                            ProductName = "Hotdog Rolls",
                            Amount = 200,
                            ProductionLine = "Production 1",
                            packaging = new Packaging(3, 1),
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Flour", IngredientAmount = 4, AdditionsAmount = 0.5, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Sugar", IngredientAmount = 2, AdditionsAmount = 0.2, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Yeast", IngredientAmount = 1, AdditionsAmount = 0.1, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Salt", IngredientAmount = 2, AdditionsAmount = 0.1, MeasuredIngredient = "bags" }
                            },
                            Notes = "Use fine grain yeast for quick rise."
                        },
                        new ProductionItem
                        {
                            ProductName = "Hamburger Rolls",
                            Amount = 150,
                            ProductionLine = "Production 2",
                            packaging = new Packaging(2, 1),
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Flour", IngredientAmount = 3, AdditionsAmount = 0.5, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Eggs", IngredientAmount = 15, AdditionsAmount = 1, MeasuredIngredient = "dozen" },
                                new Ingredients { IngredientName = "Salt", IngredientAmount = 2, AdditionsAmount = 0.2, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Butter", IngredientAmount = 2, AdditionsAmount = 0.2, MeasuredIngredient = "bags" }
                            },
                            Notes = "Glaze tops with egg wash before baking."
                        }
                    },
                    RawMaterials = new Dictionary<string, RawMaterialValue>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Flour", new RawMaterialValue(8, "bags") },
                        { "Eggs", new RawMaterialValue(16, "dozen") },
                        { "Sugar", new RawMaterialValue(2.2, "bags") },
                        { "Salt", new RawMaterialValue(4.3, "bags") },
                        { "Yeast", new RawMaterialValue(1.1, "bags") },
                        { "Butter", new RawMaterialValue(2.2, "bags") }
                    }
                },
                new OrderScaled
                {
                    OrderId = "#002",
                    Customer = "Spar",
                    OrderDate = DateTime.Now.Date,
                    Status = "Complete",
                    ProductionItems = new List<ProductionItem>
                    {
                        new ProductionItem
                        {
                            ProductName = "Hamburger Rolls",
                            Amount = 250,
                            ProductionLine = "Production 1",
                            packaging = new Packaging(4, 2, 4, 2),
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Flour", IngredientAmount = 5, AdditionsAmount = 1, MeasuredIngredient = "bags", AmountUsed = 6 },
                                new Ingredients { IngredientName = "Eggs", IngredientAmount = 20, AdditionsAmount = 1, MeasuredIngredient = "dozen", AmountUsed = 27 },
                                new Ingredients { IngredientName = "Salt", IngredientAmount = 4, AdditionsAmount = 1, MeasuredIngredient = "bags", AmountUsed = 4 },
                                new Ingredients { IngredientName = "Butter", IngredientAmount = 4, AdditionsAmount = 1, MeasuredIngredient = "bags", AmountUsed = 2 }
                            },
                            Notes = "Extra flour was used due to spillage."
                        },
                        new ProductionItem
                        {
                            ProductName = "Croissants",
                            Amount = 110,
                            ProductionLine = "Croissant Room",
                            packaging = new Packaging(4, 2, 4, 2),
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Flour", IngredientAmount = 5, AdditionsAmount = 1, MeasuredIngredient = "bags", AmountUsed = 6 },
                                new Ingredients { IngredientName = "Eggs", IngredientAmount = 20, AdditionsAmount = 1, MeasuredIngredient = "dozen", AmountUsed = 27 },
                                new Ingredients { IngredientName = "Salt", IngredientAmount = 4, AdditionsAmount = 1, MeasuredIngredient = "bags", AmountUsed = 4 }
                            },
                            Notes = "Baked to golden brown."
                        }
                    },
                    RawMaterials = new Dictionary<string, RawMaterialValue>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Flour", new RawMaterialValue(12, "bags") },
                        { "Eggs", new RawMaterialValue(48, "dozen") },
                        { "Salt", new RawMaterialValue(10, "bags") },
                        { "Butter", new RawMaterialValue(5, "bags") }
                    }
                },
                new OrderScaled
                {
                    OrderId = "#003",
                    Customer = "Woolworths",
                    OrderDate = DateTime.Now.Date,
                    Status = "Pending",
                    ProductionItems = new List<ProductionItem>
                    {
                        new ProductionItem
                        {
                            ProductName = "Sourdough Loaves",
                            Amount = 120,
                            ProductionLine = "Artisan Bakery",
                            packaging = new Packaging(6, 2),
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Bread Flour", IngredientAmount = 25, AdditionsAmount = 2, MeasuredIngredient = "kg" },
                                new Ingredients { IngredientName = "Sourdough Starter", IngredientAmount = 10, AdditionsAmount = 1, MeasuredIngredient = "kg" },
                                new Ingredients { IngredientName = "Water", IngredientAmount = 18, AdditionsAmount = 1, MeasuredIngredient = "L" },
                                new Ingredients { IngredientName = "Sea Salt", IngredientAmount = 0.5, AdditionsAmount = 0.05, MeasuredIngredient = "kg" }
                            },
                            Notes = "24-hour cold fermentation."
                        },
                        new ProductionItem
                        {
                            ProductName = "French Baguettes",
                            Amount = 80,
                            ProductionLine = "Artisan Bakery",
                            packaging = new Packaging(4, 1),
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Bread Flour", IngredientAmount = 15, AdditionsAmount = 1, MeasuredIngredient = "kg" },
                                new Ingredients { IngredientName = "Water", IngredientAmount = 10, AdditionsAmount = 0.5, MeasuredIngredient = "L" },
                                new Ingredients { IngredientName = "Yeast", IngredientAmount = 0.4, AdditionsAmount = 0.02, MeasuredIngredient = "kg" },
                                new Ingredients { IngredientName = "Salt", IngredientAmount = 0.3, AdditionsAmount = 0.02, MeasuredIngredient = "kg" }
                            },
                            Notes = "Score diagonals with razor lame."
                        }
                    },
                    RawMaterials = new Dictionary<string, RawMaterialValue>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Bread Flour", new RawMaterialValue(43, "kg") },
                        { "Water", new RawMaterialValue(29.5, "L") },
                        { "Sourdough Starter", new RawMaterialValue(11, "kg") },
                        { "Sea Salt", new RawMaterialValue(0.55, "kg") },
                        { "Yeast", new RawMaterialValue(0.42, "kg") },
                        { "Salt", new RawMaterialValue(0.32, "kg") }
                    }
                }
            };
        }

        // History (completed orders)
        public static List<OrderRow> HistoryOrders()
        {
            return new List<OrderRow>
            {
                new OrderRow { Number = "#001", Customer = "Checkers", Date = new DateTime(2026, 5, 9), Status = "Complete" },
                new OrderRow { Number = "#002", Customer = "Spar",     Date = new DateTime(2026, 5, 9), Status = "Complete" }
            };
        }

        // The "User" role's orders
        public static List<OrderRow> UserOrders()
        {
            return new List<OrderRow>
            {
                new OrderRow { Number = "#001", Customer = "Checkers", Date = new DateTime(2026, 5, 9), Status = "Complete" },
                new OrderRow { Number = "#002", Customer = "Spar",     Date = new DateTime(2026, 5, 9), Status = "Pending" }
            };
        }

        public static List<ProductRow> Products()
        {
            return new List<ProductRow>
            {
                new ProductRow { Name = "Hamburger Rolls", Price = "R24.99", SellBy = "5", BestBefore = "5", Storage = "Freezer" },
                new ProductRow { Name = "Hotdog Rolls",    Price = "R24.99", SellBy = "6", BestBefore = "6", Storage = "Freezer" }
            };
        }

        public static List<UserRow> Users()
        {
            return new List<UserRow>
            {
                new UserRow { Id = "01", Name = "Blessings",        Role = "Admin",   DateAdded = "12/08/2026" },
                new UserRow { Id = "02", Name = "Paul",             Role = "Manager", DateAdded = "12/08/2026" },
                new UserRow { Id = "03", Name = "Bread Station #1", Role = "User",    DateAdded = "12/08/2026" }
            };
        }

        // Adriaan - Sample Order Summary and Sheet Generation
        public static SummaryData Summary(bool completed)
        {
            return new SummaryData
            {
                Title = "Spar Order #002",
                DateText = "5 August 2026",
                IsCompleted = completed,
                Lines = new List<SummaryLine>
                {
                    new SummaryLine
                    {
                        Product = "250 Hamburger Rolls", Production = "Production 1",
                        Ingredients = "Flour: 6 bags,   Eggs: 27 dozen,   Salt: 3 bags",
                        Packaging = "Pans: 2,   Trolleys: 1"
                    },
                    new SummaryLine
                    {
                        Product = "100 Rolls", Production = "Production 1",
                        Ingredients = "Flour: 6 bags,   Eggs: 27 dozen,   Salt: 3 bags",
                        Packaging = "Pans: 2,   Trolleys: 1"
                    }
                }
            };
        }

        public static RawMaterialData RawMaterials()
        {
            return new RawMaterialData
            {
                Title = "Spar Order #002",
                DateText = "5 August 2026",
                Totals = new List<string> { "Eggs: 50 dozen", "Flour: 100 bags", "Salt: 50 bags" }
            };
        }

        /// <summary>
        /// The production sheet. filled = true gives the completed version (Used values in
        /// green / red, notes filled in); false gives the blank one a baker fills in.
        /// </summary>
        public static SheetData Sheet(bool filled)
        {
            Brush good = filled ? Palette.UserComplete : Palette.Black;
            Brush bad = filled ? Palette.UserPending : Palette.Black;

            return new SheetData
            {
                Title = "Spar Order #002",
                DateText = "5 August 2026",
                IsCompleted = filled,
                Products = new List<SheetProduct>
                {
                    new SheetProduct
                    {
                        Name = "Hamburger Rolls", Amount = "250", Production = "Production 1",
                        Ingredients = new List<SheetIngredient>
                        {
                            new SheetIngredient { Name = "Flour",  Amount = "5 bags",   Additional = "6 bags",   Used = filled ? "6 bags"   : "", UsedBrush = good },
                            new SheetIngredient { Name = "Eggs",   Amount = "20 dozen", Additional = "21 dozen", Used = filled ? "27 dozen" : "", UsedBrush = bad },
                            new SheetIngredient { Name = "Salt",   Amount = "4 bags",   Additional = "5 bags",   Used = filled ? "4 bags"   : "", UsedBrush = good },
                            new SheetIngredient { Name = "Butter", Amount = "4 bags",   Additional = "5 bags",   Used = filled ? "2 bags"   : "", UsedBrush = good }
                        },
                        Packaging = new List<SheetPackaging>
                        {
                            new SheetPackaging { Name = "Pans",     Amount = "4", Used = filled ? "4" : "", UsedBrush = good },
                            new SheetPackaging { Name = "Trolleys", Amount = "2", Used = filled ? "2" : "", UsedBrush = good }
                        },
                        Notes = filled ? "Extra flour was used due to spillage." : ""
                    },
                    new SheetProduct
                    {
                        Name = "Croissants", Amount = "110", Production = "Croissant Room",
                        Ingredients = new List<SheetIngredient>
                        {
                            new SheetIngredient { Name = "Flour", Amount = "5 bags",   Additional = "6 bags",   Used = filled ? "6 bags"   : "", UsedBrush = good },
                            new SheetIngredient { Name = "Eggs",  Amount = "20 dozen", Additional = "21 dozen", Used = filled ? "27 dozen" : "", UsedBrush = bad },
                            new SheetIngredient { Name = "Salt",  Amount = "4 bags",   Additional = "5 bags",   Used = filled ? "4 bags"   : "", UsedBrush = good }
                        },
                        Packaging = new List<SheetPackaging>
                        {
                            new SheetPackaging { Name = "Pans",     Amount = "4", Used = filled ? "4" : "", UsedBrush = good },
                            new SheetPackaging { Name = "Trolleys", Amount = "2", Used = filled ? "2" : "", UsedBrush = good }
                        },
                        Notes = ""
                    }
                }
            };
        }

        public static ProductDetail DetailFor(ProductRow row)
        {
            return new ProductDetail
            {
                Name = row.Name,
                DateAdded = "10 August 2026",
                Ingredients = new List<IngredientLine>
                {
                    new IngredientLine { Name = "Eggs",  Quantity = "5" },
                    new IngredientLine { Name = "Flour", Quantity = "5 Cups" },
                    new IngredientLine { Name = "Salt",  Quantity = "1 Cup" }
                },
                Method = "Mix eggs, flour and salt until combine. Place on baking tray and put into oven at 180 degrees for 20 minutes.",
                Storage = row.Storage + " Room 1"
            };
        }
    }
}