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
using System.Text.Json.Serialization;

#if WINDOWS
using System.Windows.Media;
#else
using ImageSource = System.Object;
using Brush = System.Object;
#endif

namespace SharedLibrary.Model
{
    // =====================================================================
    //  Front-end models for WPF and shared across applications.
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
        public static OrderRow FromOrderScaled(OrderScaled? order)
        {
            if (order == null)
            {
                return new OrderRow
                {
                    Number = string.Empty,
                    Customer = string.Empty,
                    Date = DateTime.Now,
                    Status = "Pending"
                };
            }

            return new OrderRow
            {
                Number = order.OrderId ?? string.Empty,
                Customer = order.Customer ?? string.Empty,
                Date = order.OrderDate,
                Status = order.Status == "Completed" ? "Complete" : (order.Status ?? "Pending")
            };
        }
    }

    public class ProductRow
    {
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
            if (order == null) return new SheetData();

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
        public static SummaryData FromOrderScaled(OrderScaled? order)
        {
            if (order == null) return new SummaryData();

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

        public static RawMaterialData FromOrderScaled(OrderScaled? order)
        {
            if (order == null) return new RawMaterialData();

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
        public string Name { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
    }

    public class ProductDetail
    {
        public string Name { get; set; } = string.Empty;
        public string DateAdded { get; set; } = string.Empty;
        public List<IngredientLine> Ingredients { get; set; } = new();
        public string Method { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
    }
}
