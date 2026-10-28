using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AndersonsBakeryAPI.Services
{
    // Service responsible for reading and parsing Excel (.xlsx/.xls/.csv) production order sheets
    public class ExcelOrderImportService
    {
        // Imports actual used quantities and notes from an Excel file into an existing OrderScaled object
        public bool ImportOrderFromExcel(string filePath, OrderScaled targetOrder)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            if (targetOrder == null)
                throw new ArgumentNullException(nameof(targetOrder));

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

                    // Process rows based on current section
                    if (currentSection == 1)
                    {
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

                // If missing or empty, calculate raw materials summary from production items
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
                try { NullLogger.Instance.LogWarning(ex, "Excel import error"); } catch { }
                return false;
            }
        }

        // Reads an OrderScaled object from an Excel file, extracting metadata and production items
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

                ImportOrderFromExcel(filePath, order);
                return order;
            }
            catch
            {
                return null;
            }
        }

        // Tries to parse a string into a double, handling both comma and dot as decimal separators
        public static bool TryParseDouble(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string clean = text.Trim().Replace(',', '.');
            return double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        // Loads workbook from Excel (.xlsx/.xls) or converts CSV into in-memory XLWorkbook
        public static XLWorkbook LoadWorkbook(string filePath)
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
