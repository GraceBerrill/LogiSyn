using ClosedXML.Excel;
using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AndersonsBakeryAPI.Services
{
    // Service responsible for generating and formatting Excel (.xlsx) production sheets
    public class ExcelOrderExportService
    {
        // Exports an OrderScaled object to an Excel file (.xlsx) with a structured layout
        public string ExportOrderToExcel(OrderScaled order, string? outputFilePath = null)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string outDir = Path.Combine(docs, "LogiSyn_Orders");
                Directory.CreateDirectory(outDir);

                string safeOrderId = string.Join("_", (order.OrderId ?? "001").Split(Path.GetInvalidFileNameChars()));
                string safeCustomer = string.Join("_", (order.Customer ?? "Customer").Split(Path.GetInvalidFileNameChars()));
                outputFilePath = Path.Combine(outDir, $"Order_{safeOrderId}_{safeCustomer}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }

            string? dir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Production Sheet");
            WriteOrderToWorksheet(ws, order);
            workbook.SaveAs(outputFilePath);

            return outputFilePath;
        }

        // Exports multiple orders to a single Excel file (.xlsx) with an overview sheet and dedicated tabs for each order
        public string ExportOrdersToExcel(IEnumerable<OrderScaled> orders, string? outputFilePath = null)
        {
            var orderList = orders?.ToList() ?? new List<OrderScaled>();
            if (orderList.Count == 0)
                throw new ArgumentException("No orders to export.", nameof(orders));

            if (orderList.Count == 1)
            {
                return ExportOrderToExcel(orderList[0], outputFilePath);
            }

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string outDir = Path.Combine(docs, "LogiSyn_Orders");
                Directory.CreateDirectory(outDir);
                outputFilePath = Path.Combine(outDir, $"Production_Orders_{orderList.Count}_Orders_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }

            string? dir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var workbook = new XLWorkbook();

            // Sheet 1: Orders Overview summary
            var summaryWs = workbook.Worksheets.Add("Orders Overview");
            summaryWs.Cell(1, 1).Value = "ANDERSON'S BAKERY - PRODUCTION ORDERS OVERVIEW";
            summaryWs.Range(1, 1, 1, 7).Merge();
            summaryWs.Cell(1, 1).Style.Font.Bold = true;
            summaryWs.Cell(1, 1).Style.Font.FontSize = 16;
            summaryWs.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            summaryWs.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2136");
            summaryWs.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            summaryWs.Cell(3, 1).Value = "Export Date:";
            summaryWs.Cell(3, 1).Style.Font.Bold = true;
            summaryWs.Cell(3, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            summaryWs.Cell(3, 4).Value = "Total Orders:";
            summaryWs.Cell(3, 4).Style.Font.Bold = true;
            summaryWs.Cell(3, 5).Value = orderList.Count;

            int sRow = 5;
            string[] sHeaders = { "Order ID", "Customer", "Date", "Status", "Products Count", "Pans (Req)", "Trolleys (Req)" };
            for (int c = 0; c < sHeaders.Length; c++)
            {
                var cell = summaryWs.Cell(sRow, c + 1);
                cell.Value = sHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2A334E");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            double grandPans = 0;
            double grandTrolleys = 0;
            int grandProducts = 0;

            foreach (var ord in orderList)
            {
                sRow++;
                var pItems = ord.productionItems ?? new List<ProductionItem>();
                double ordPans = pItems.Sum(p => p.packaging?.Pans ?? 0);
                double ordTrolleys = pItems.Sum(p => p.packaging?.Trolleys ?? 0);
                grandPans += ordPans;
                grandTrolleys += ordTrolleys;
                grandProducts += pItems.Count;

                summaryWs.Cell(sRow, 1).Value = ord.OrderId;
                summaryWs.Cell(sRow, 2).Value = ord.Customer;
                summaryWs.Cell(sRow, 3).Value = ord.OrderDate.ToString("dd/MM/yyyy");
                summaryWs.Cell(sRow, 4).Value = string.IsNullOrWhiteSpace(ord.Status) ? "Pending" : ord.Status;
                summaryWs.Cell(sRow, 5).Value = pItems.Count;
                summaryWs.Cell(sRow, 6).Value = ordPans;
                summaryWs.Cell(sRow, 7).Value = ordTrolleys;

                for (int c = 1; c <= 7; c++)
                {
                    summaryWs.Cell(sRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            sRow++;
            summaryWs.Cell(sRow, 1).Value = "TOTAL";
            summaryWs.Cell(sRow, 1).Style.Font.Bold = true;
            summaryWs.Range(sRow, 1, sRow, 4).Merge();
            summaryWs.Cell(sRow, 5).Value = grandProducts;
            summaryWs.Cell(sRow, 5).Style.Font.Bold = true;
            summaryWs.Cell(sRow, 6).Value = grandPans;
            summaryWs.Cell(sRow, 6).Style.Font.Bold = true;
            summaryWs.Cell(sRow, 7).Value = grandTrolleys;
            summaryWs.Cell(sRow, 7).Style.Font.Bold = true;
            for (int c = 1; c <= 7; c++)
            {
                summaryWs.Cell(sRow, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                summaryWs.Cell(sRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            summaryWs.Columns().AdjustToContents();

            // Each order gets its own detailed sheet
            foreach (var ord in orderList)
            {
                string safeId = (ord.OrderId ?? "001").Replace("#", "").Trim();
                string safeCustomer = (ord.Customer ?? "Customer").Trim();
                string rawName = $"Order {safeId} - {safeCustomer}";
                char[] invalid = new[] { '\\', '/', '?', '*', '[', ']', ':', '\'', '"' };
                foreach (var c in invalid) rawName = rawName.Replace(c, '_');
                if (rawName.Length > 28) rawName = rawName.Substring(0, 28);
                string sheetName = rawName;
                int counter = 1;
                while (workbook.Worksheets.Any(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase)))
                {
                    sheetName = $"{rawName.Substring(0, Math.Min(rawName.Length, 25))}_{counter++}";
                }

                var ws = workbook.Worksheets.Add(sheetName);
                WriteOrderToWorksheet(ws, ord);
            }

            workbook.SaveAs(outputFilePath);
            return outputFilePath;
        }

        // Exports each order to its own individual .xlsx file in the specified output folder
        public List<string> ExportOrdersToIndividualFiles(IEnumerable<OrderScaled> orders, string outputFolder)
        {
            var files = new List<string>();
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LogiSyn_Orders");
            }
            Directory.CreateDirectory(outputFolder);

            foreach (var ord in orders ?? Enumerable.Empty<OrderScaled>())
            {
                string safeOrderId = string.Join("_", (ord.OrderId ?? "001").Split(Path.GetInvalidFileNameChars()));
                string safeCustomer = string.Join("_", (ord.Customer ?? "Customer").Split(Path.GetInvalidFileNameChars()));
                string path = Path.Combine(outputFolder, $"Order_{safeOrderId}_{safeCustomer}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                ExportOrderToExcel(ord, path);
                files.Add(path);
            }

            return files;
        }

        // Populates an Excel worksheet with the complete, structured 3-section production order
        public void WriteOrderToWorksheet(IXLWorksheet ws, OrderScaled order)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            if (order == null) throw new ArgumentNullException(nameof(order));

            // Formatting and styling of excel sheet
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

            // PRODUCTS & PACKAGING
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

            // INGREDIENTS & ACTUAL AMOUNTS
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

            // RAW MATERIAL TOTALS
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
        }
    }
}
