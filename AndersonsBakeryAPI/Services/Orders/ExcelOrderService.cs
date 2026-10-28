using ClosedXML.Excel;
using SharedLibrary.Model;
using System.Collections.Generic;

namespace AndersonsBakeryAPI.Services
{
    // Facade service coordinating Excel order generation, export formatting, and spreadsheet importing
    public class ExcelOrderService
    {
        private readonly ExcelOrderExportService _exportService;
        private readonly ExcelOrderImportService _importService;

        public ExcelOrderService(
            ExcelOrderExportService? exportService = null,
            ExcelOrderImportService? importService = null)
        {
            _exportService = exportService ?? new ExcelOrderExportService();
            _importService = importService ?? new ExcelOrderImportService();
        }

        // Exports an OrderScaled object to an Excel file (.xlsx) with a structured layout
        public string ExportOrderToExcel(OrderScaled order, string? outputFilePath = null)
            => _exportService.ExportOrderToExcel(order, outputFilePath);

        // Exports multiple orders to a single Excel file (.xlsx) with an overview sheet and dedicated tabs for each order
        public string ExportOrdersToExcel(IEnumerable<OrderScaled> orders, string? outputFilePath = null)
            => _exportService.ExportOrdersToExcel(orders, outputFilePath);

        // Exports each order to its own individual .xlsx file in the specified output folder
        public List<string> ExportOrdersToIndividualFiles(IEnumerable<OrderScaled> orders, string outputFolder)
            => _exportService.ExportOrdersToIndividualFiles(orders, outputFolder);

        // Populates an Excel worksheet with the complete, structured 3-section production order
        public void WriteOrderToWorksheet(IXLWorksheet ws, OrderScaled order)
            => _exportService.WriteOrderToWorksheet(ws, order);

        // Imports actual used quantities and notes from an Excel file into an existing OrderScaled object
        public bool ImportOrderFromExcel(string filePath, OrderScaled targetOrder)
            => _importService.ImportOrderFromExcel(filePath, targetOrder);

        // Reads an OrderScaled object from an Excel file, extracting metadata and production items
        public OrderScaled? ReadOrderFromExcel(string filePath)
            => _importService.ReadOrderFromExcel(filePath);
    }
}
