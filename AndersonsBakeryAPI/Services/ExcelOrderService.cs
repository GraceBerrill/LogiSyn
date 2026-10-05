// Adriaan
using ClosedXML.Excel;
using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AndersonsBakeryAPI.Services
{
    /// <summary>
    /// Service for exporting production orders to Excel (.xlsx) and importing
    /// baker-edited Excel files with actual used quantities and notes.
    /// </summary>
    public class ExcelOrderService
    {
        //------------------------------------------------------------------------------------------------//

        // Exports an OrderScaled object to an Excel file (.xlsx) with a structured layout.
        public string ExportOrderToExcel(OrderScaled order, string? outputFilePath = null)
        {
            // Validate input
            if (order == null) throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                // Default output path: My Documents\LogiSyn_Orders\ with timestamped filename
                //TODO: Consider using a more robust path for production, like saving to mongodb
                // It will have to be able to update the order in the database with the new values
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string outDir = Path.Combine(docs, "LogiSyn_Orders");
                Directory.CreateDirectory(outDir);

                // Sanitize OrderId for filename
                string safeOrderId = string.Join("_", (order.OrderId ?? "001").Split(Path.GetInvalidFileNameChars()));
                outputFilePath = Path.Combine(outDir, $"Order_{safeOrderId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }

            // Ensure the output directory exists
            string? dir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Production Sheet");

            // formatting and styling of excel sheet
            ws.Cell(1, 1).Value = "ANDERSON'S BAKERY - PRODUCTION ORDER";
            ws.Range(1, 1, 1, 8).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2136");
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Order Metadata Header
            ws.Cell(3, 1).Value = "Order ID:";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 2).Value = order.OrderId;

            ws.Cell(3, 3).Value = "Customer:";
            ws.Cell(3, 3).Style.Font.Bold = true;
            ws.Cell(3, 4).Value = order.Customer;

            ws.Cell(3, 5).Value = "Date:";
            ws.Cell(3, 5).Style.Font.Bold = true;
            ws.Cell(3, 6).Value = order.OrderDate.ToString("dd/MM/yyyy");

            ws.Cell(3, 7).Value = "Status:";
            ws.Cell(3, 7).Style.Font.Bold = true;
            ws.Cell(3, 8).Value = string.IsNullOrWhiteSpace(order.Status) ? "Pending" : order.Status;

            // SECTION 1: PRODUCTS & PACKAGING
            int row = 5;
            ws.Cell(row, 1).Value = "1. PRODUCTS & PACKAGING";
            ws.Range(row, 1, row, 7).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 13;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2A334E");
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;

            row++;
            string[] prodHeaders = { "Product Name", "Line", "Pans (Req)", "Pans Used (Baker)", "Trolleys (Req)", "Trolleys Used (Baker)", "Baker Notes" };
            for (int c = 0; c < prodHeaders.Length; c++)
            {
                // Create header cells with styling
                var cell = ws.Cell(row, c + 1);
                cell.Value = prodHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            foreach (var item in order.productionItems ?? new List<ProductionItem>())
            {
                row++;
                ws.Cell(row, 1).Value = item.ProductName;
                ws.Cell(row, 2).Value = item.ProductionLine;
                ws.Cell(row, 3).Value = item.packaging.Pans;

                // Pans Used (Editable input)
                var cellPansUsed = ws.Cell(row, 4);
                if (item.packaging.PansUsed > 0) cellPansUsed.Value = item.packaging.PansUsed;
                cellPansUsed.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF9E6");
                cellPansUsed.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                ws.Cell(row, 5).Value = item.packaging.Trolleys;

                // Trolleys Used (Editable input)
                var cellTrolleysUsed = ws.Cell(row, 6);
                if (item.packaging.TrolleysUsed > 0) cellTrolleysUsed.Value = item.packaging.TrolleysUsed;
                cellTrolleysUsed.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF9E6");
                cellTrolleysUsed.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // Notes (Editable input)
                var cellNotes = ws.Cell(row, 7);
                cellNotes.Value = item.Notes ?? "";
                cellNotes.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF9E6");
                cellNotes.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // SECTION 2: INGREDIENTS & ACTUAL AMOUNTS
            row += 2;
            ws.Cell(row, 1).Value = "2. INGREDIENTS & ACTUAL AMOUNTS USED";
            ws.Range(row, 1, row, 7).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 13;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2A334E");
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;

            row++;
            string[] ingHeaders = { "Product Name", "Ingredient", "Required", "Additional", "Unit", "Total Planned", "Amount Used (Baker)" };
            for (int c = 0; c < ingHeaders.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = ingHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            foreach (var item in order.productionItems ?? new List<ProductionItem>())
            {
                foreach (var ing in item.ReqIngredients ?? new List<Ingredients>())
                {
                    row++;
                    ws.Cell(row, 1).Value = item.ProductName;
                    ws.Cell(row, 2).Value = ing.IngredientName;
                    ws.Cell(row, 3).Value = ing.IngredientAmount;
                    ws.Cell(row, 4).Value = ing.AdditionsAmount;
                    ws.Cell(row, 5).Value = ing.MeasuredIngredient;
                    ws.Cell(row, 6).Value = ing.IngredientAmount + ing.AdditionsAmount;

                    // Amount Used (Editable input)
                    var cellUsed = ws.Cell(row, 7);
                    if (ing.AmountUsed > 0) cellUsed.Value = ing.AmountUsed;
                    cellUsed.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF9E6");
                    cellUsed.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            // SECTION 3: RAW MATERIAL TOTALS
            if (order.RawMaterials != null && order.RawMaterials.Count > 0)
            {
                row += 2;
                ws.Cell(row, 1).Value = "3. RAW MATERIAL SCALING SUMMARY";
                ws.Range(row, 1, row, 3).Merge();
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 13;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2A334E");
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;

                row++;
                ws.Cell(row, 1).Value = "Material";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                ws.Cell(row, 2).Value = "Total Amount";
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                ws.Cell(row, 3).Value = "Unit";
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                foreach (var kvp in order.RawMaterials)
                {
                    row++;
                    ws.Cell(row, 1).Value = kvp.Key;
                    ws.Cell(row, 2).Value = kvp.Value.Amount;
                    ws.Cell(row, 3).Value = kvp.Value.Unit;
                }
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(outputFilePath);

            return outputFilePath;
        }

        //------------------------------------------------------------------------------------------------//

        // Imports actual used quantities and notes from an Excel file into an existing OrderScaled object for admin
        public bool ImportOrderFromExcel(string filePath, OrderScaled targetOrder)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            if (targetOrder == null)
                throw new ArgumentNullException(nameof(targetOrder));

            // Read the Excel file and update the targetOrder with actual used quantities and notes
            try
            {
                using var workbook = LoadWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws == null) return false;

                // Look for Order ID in header
                for (int r = 1; r <= 5; r++)
                {
                    for (int c = 1; c <= 8; c++)
                    {
                        string label = ws.Cell(r, c).GetString().Trim();
                        if (label.Equals("Order ID:", StringComparison.OrdinalIgnoreCase) && c + 1 <= 8)
                        {
                            string fileOrderId = ws.Cell(r, c + 1).GetString().Trim();
                            if (!string.IsNullOrEmpty(fileOrderId) && string.IsNullOrEmpty(targetOrder.OrderId))
                            {
                                targetOrder.OrderId = fileOrderId;
                            }
                        }
                    }
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 100;
                int currentSection = 0;

                // Loop through rows to find sections and extract data
                for (int r = 1; r <= lastRow; r++)
                {
                    string firstCell = ws.Cell(r, 1).GetString().Trim();

                    if (firstCell.StartsWith("1. PRODUCTS", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = 1;
                        r++;
                        continue;
                    }
                    else if (firstCell.StartsWith("2. INGREDIENTS", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = 2;
                        r++;
                        continue;
                    }
                    else if (firstCell.StartsWith("3. RAW MATERIAL", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = 3;
                        r++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(firstCell)) continue;

                    // Process rows based on the current section
                    if (currentSection == 1)
                    {
                        // parsed product row information
                        string prodName = firstCell;
                        if (prodName.Equals("Product Name", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string lineName = ws.Cell(r, 2).GetString().Trim();
                        string pansReqStr = ws.Cell(r, 3).GetString().Trim();
                        string pansUsedStr = ws.Cell(r, 4).GetString().Trim();
                        string trolleysReqStr = ws.Cell(r, 5).GetString().Trim();
                        string trolleysUsedStr = ws.Cell(r, 6).GetString().Trim();
                        string notes = ws.Cell(r, 7).GetString().Trim();

                        TryParseDouble(pansReqStr, out double pansReq);
                        TryParseDouble(pansUsedStr, out double pansUsed);
                        TryParseDouble(trolleysReqStr, out double trolleysReq);
                        TryParseDouble(trolleysUsedStr, out double trolleysUsed);

                        var pItem = targetOrder.productionItems?.FirstOrDefault(p =>
                            p.ProductName.Trim().Equals(prodName, StringComparison.OrdinalIgnoreCase) ||
                            prodName.Contains(p.ProductName, StringComparison.OrdinalIgnoreCase) ||
                            p.ProductName.Contains(prodName, StringComparison.OrdinalIgnoreCase));

                        if (pItem != null)
                        {
                            if (pansReq > 0) pItem.packaging.Pans = pansReq;
                            if (pansUsed > 0) pItem.packaging.PansUsed = pansUsed;
                            if (trolleysReq > 0) pItem.packaging.Trolleys = trolleysReq;
                            if (trolleysUsed > 0) pItem.packaging.TrolleysUsed = trolleysUsed;
                            if (!string.IsNullOrWhiteSpace(lineName) && string.IsNullOrWhiteSpace(pItem.ProductionLine))
                                pItem.ProductionLine = lineName;
                            if (!string.IsNullOrEmpty(notes)) pItem.Notes = notes;
                        }
                        else
                        {
                            string cleanProdName = prodName;
                            int parsedAmount = 100;
                            var match = Regex.Match(prodName, @"^(\d+)\s+(.+)$");
                            if (match.Success)
                            {
                                if (int.TryParse(match.Groups[1].Value, out int amt))
                                {
                                    parsedAmount = amt;
                                }
                                cleanProdName = match.Groups[2].Value.Trim();
                            }
                            else if (pansReq > 0)
                            {
                                parsedAmount = (int)(pansReq * 50);
                            }

                            pItem = new ProductionItem
                            {
                                ProductName = cleanProdName,
                                Amount = parsedAmount,
                                ProductionLine = !string.IsNullOrWhiteSpace(lineName) ? lineName : "Production 1",
                                packaging = new Packaging
                                {
                                    Pans = pansReq,
                                    PansUsed = pansUsed,
                                    Trolleys = trolleysReq,
                                    TrolleysUsed = trolleysUsed
                                },
                                Notes = notes
                            };
                            targetOrder.ProductionItems.Add(pItem);
                        }
                    }
                    else if (currentSection == 2)
                    {
                        // parsed ingredient row information
                        string prodName = firstCell;
                        if (prodName.Equals("Product Name", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string ingName = ws.Cell(r, 2).GetString().Trim();
                        string reqStr = ws.Cell(r, 3).GetString().Trim();
                        string addStr = ws.Cell(r, 4).GetString().Trim();
                        string unitStr = ws.Cell(r, 5).GetString().Trim();
                        string usedStr = ws.Cell(r, 7).GetString().Trim();

                        var pItem = targetOrder.productionItems?.FirstOrDefault(p =>
                            p.ProductName.Trim().Equals(prodName, StringComparison.OrdinalIgnoreCase) ||
                            prodName.Contains(p.ProductName, StringComparison.OrdinalIgnoreCase) ||
                            p.ProductName.Contains(prodName, StringComparison.OrdinalIgnoreCase));

                        if (pItem != null && !string.IsNullOrWhiteSpace(ingName))
                        {
                            if (pItem.ReqIngredients == null)
                            {
                                pItem.ReqIngredients = new List<Ingredients>();
                            }

                            var ing = pItem.ReqIngredients.FirstOrDefault(i =>
                                i.IngredientName.Trim().Equals(ingName, StringComparison.OrdinalIgnoreCase));

                            TryParseDouble(reqStr, out double reqVal);
                            TryParseDouble(addStr, out double addVal);
                            TryParseDouble(usedStr, out double usedVal);

                            if (ing == null)
                            {
                                ing = new Ingredients
                                {
                                    IngredientName = ingName,
                                    IngredientAmount = reqVal,
                                    AdditionsAmount = addVal,
                                    MeasuredIngredient = !string.IsNullOrWhiteSpace(unitStr) ? unitStr : "kg",
                                    AmountUsed = usedVal
                                };
                                pItem.ReqIngredients.Add(ing);
                            }
                            else
                            {
                                if (reqVal > 0) ing.IngredientAmount = reqVal;
                                if (addVal > 0) ing.AdditionsAmount = addVal;
                                if (!string.IsNullOrWhiteSpace(unitStr)) ing.MeasuredIngredient = unitStr;
                                if (usedVal > 0) ing.AmountUsed = usedVal;
                            }
                        }
                    }
                    else if (currentSection == 3)
                    {
                        string materialName = firstCell;
                        if (materialName.Equals("Material", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string amountStr = ws.Cell(r, 2).GetString().Trim();
                        string unitStr = ws.Cell(r, 3).GetString().Trim();

                        if (!string.IsNullOrWhiteSpace(materialName))
                        {
                            targetOrder.RawMaterials ??= new Dictionary<string, RawMaterialValue>(StringComparer.OrdinalIgnoreCase);

                            if (TryParseDouble(amountStr, out double totalAmt))
                            {
                                targetOrder.RawMaterials[materialName] = new RawMaterialValue(
                                    totalAmt,
                                    !string.IsNullOrWhiteSpace(unitStr) ? unitStr : "kg"
                                );
                            }
                        }
                    }
                }

                // If Section 3 was missing or empty, calculate raw materials summary from production items
                if (targetOrder.RawMaterials == null || targetOrder.RawMaterials.Count == 0)
                {
                    var aggregates = new Dictionary<string, (double Amount, string Unit)>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in targetOrder.productionItems ?? new List<ProductionItem>())
                    {
                        foreach (var ing in item.ReqIngredients ?? new List<Ingredients>())
                        {
                            if (string.IsNullOrWhiteSpace(ing.IngredientName)) continue;
                            string key = ing.IngredientName.Trim();
                            double total = ing.IngredientAmount + ing.AdditionsAmount;
                            string unit = !string.IsNullOrWhiteSpace(ing.MeasuredIngredient) ? ing.MeasuredIngredient : "kg";

                            if (aggregates.ContainsKey(key))
                                aggregates[key] = (aggregates[key].Amount + total, aggregates[key].Unit);
                            else
                                aggregates[key] = (total, unit);
                        }
                    }
                    targetOrder.RawMaterials = aggregates.ToDictionary(kvp => kvp.Key, kvp => new RawMaterialValue(kvp.Value.Amount, kvp.Value.Unit));
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExcelOrderService] Import error: {ex.Message}");
                return false;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Reads an OrderScaled object from an Excel file, extracting metadata and production items.
        public OrderScaled? ReadOrderFromExcel(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                using var workbook = LoadWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws == null) return null;

                var order = new OrderScaled
                {
                    OrderId = "#001",
                    Customer = "Customer",
                    OrderDate = DateTime.Now,
                    Status = "In Production",
                    productionItems = new List<ProductionItem>()
                };

                // Read metadata
                for (int r = 1; r <= 5; r++)
                {
                    for (int c = 1; c <= 8; c++)
                    {
                        string label = ws.Cell(r, c).GetString().Trim();
                        if (label.Equals("Order ID:", StringComparison.OrdinalIgnoreCase))
                            order.OrderId = ws.Cell(r, c + 1).GetString().Trim();
                        else if (label.Equals("Customer:", StringComparison.OrdinalIgnoreCase))
                            order.Customer = ws.Cell(r, c + 1).GetString().Trim();
                        else if (label.Equals("Date:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (DateTime.TryParse(ws.Cell(r, c + 1).GetString().Trim(), out var dt))
                                order.OrderDate = dt;
                        }
                    }
                }

                // Call import to populate items
                ImportOrderFromExcel(filePath, order);
                return order;
            }
            catch
            {
                return null;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Tries to parse a string into a double, handling both comma and dot as decimal separators.
        private static bool TryParseDouble(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string clean = text.Trim().Replace(',', '.');
            return double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        // Loads workbook from Excel (.xlsx/.xls) or converts CSV into in-memory XLWorkbook
        private static XLWorkbook LoadWorkbook(string filePath)
        {
            if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Production Sheet");
                var lines = File.ReadAllLines(filePath);
                for (int r = 0; r < lines.Length; r++)
                {
                    var cols = lines[r].Split(new[] { ',', ';' });
                    for (int c = 0; c < cols.Length; c++)
                    {
                        ws.Cell(r + 1, c + 1).Value = cols[c].Trim(' ', '"');
                    }
                }
                return wb;
            }
            return new XLWorkbook(filePath);
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//

