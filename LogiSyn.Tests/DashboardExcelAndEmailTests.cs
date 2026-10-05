using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AndersonsBakeryAPI.Services;
using ClosedXML.Excel;
using LogiSyn.Views;
using SharedLibrary.Model;
using Xunit;

namespace LogiSyn.Tests
{
    public class DashboardExcelAndEmailTests
    {
        [Fact]
        // test shows that smaple order contains details
        public void SampleOrdersScaled_ContainsRichDetailsForCheckersAndSpar()
        {
            var samples = TestOrderFactory.CreateTestOrders();
            Assert.NotNull(samples);
            Assert.True(samples.Count >= 2);

            var checkers = samples.FirstOrDefault(s => s.Customer == "Checkers");
            Assert.NotNull(checkers);
            Assert.NotEmpty(checkers.ProductionItems);
            Assert.NotEmpty(checkers.RawMaterials);

            var spar = samples.FirstOrDefault(s => s.Customer == "Spar");
            Assert.NotNull(spar);
            Assert.NotEmpty(spar.ProductionItems);
            Assert.NotEmpty(spar.RawMaterials);

            // Spar order must have complete details (not just empty stub)
            var sparItem = spar.ProductionItems[0];
            Assert.False(string.IsNullOrWhiteSpace(sparItem.ProductName));
            Assert.NotEmpty(sparItem.ReqIngredients);
            Assert.True(sparItem.Packaging.Pans > 0);
            Assert.True(spar.RawMaterials.Count > 0);
        }

        [Fact]
        public void ExportOrdersToExcel_MultiOrder_CreatesOverviewAndDedicatedSheetsWithDetails()
        {
            var excelService = new ExcelOrderService();
            var orders = TestOrderFactory.CreateTestOrders();
            var tempFile = Path.Combine(Path.GetTempPath(), $"multi_order_test_{Guid.NewGuid():N}.xlsx");

            try
            {
                string path = excelService.ExportOrdersToExcel(orders, tempFile);
                Assert.True(File.Exists(path));

                using var workbook = new XLWorkbook(path);
                // Must have Orders Overview sheet + sheet for each order
                Assert.True(workbook.Worksheets.Count >= orders.Count + 1);

                var overviewWs = workbook.Worksheet("Orders Overview");
                Assert.NotNull(overviewWs);
                Assert.Equal("ANDERSON'S BAKERY - PRODUCTION ORDERS OVERVIEW", overviewWs.Cell(1, 1).GetString());

                // Check Spar and Checkers sheets
                foreach (var ord in orders)
                {
                    var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains(ord.Customer));
                    Assert.NotNull(sheet);

                    // Verify Title banner
                    Assert.Equal("ANDERSON'S BAKERY - PRODUCTION ORDER", sheet.Cell(1, 1).GetString());

                    // Verify metadata
                    Assert.Equal(ord.OrderId, sheet.Cell(3, 2).GetString());
                    Assert.Equal(ord.Customer, sheet.Cell(3, 4).GetString());

                    // Verify Section 1 Products & Packaging
                    bool hasSection1 = false;
                    bool hasSection2 = false;
                    bool hasSection3 = false;
                    bool hasProductDetail = false;

                    for (int r = 4; r <= 35; r++)
                    {
                        string val = sheet.Cell(r, 1).GetString();
                        if (val.Contains("1. PRODUCTS & PACKAGING")) hasSection1 = true;
                        if (val.Contains("2. INGREDIENTS")) hasSection2 = true;
                        if (val.Contains("3. RAW MATERIAL")) hasSection3 = true;

                        if (ord.ProductionItems.Any(p => p.ProductName == val))
                        {
                            hasProductDetail = true;
                        }
                    }

                    Assert.True(hasSection1, $"Order {ord.OrderId} sheet missing Section 1");
                    Assert.True(hasSection2, $"Order {ord.OrderId} sheet missing Section 2");
                    Assert.True(hasSection3, $"Order {ord.OrderId} sheet missing Section 3");
                    Assert.True(hasProductDetail, $"Order {ord.OrderId} sheet missing product rows");
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void ExportOrdersToIndividualFiles_ExportsAllOrdersIntoFolder()
        {
            var excelService = new ExcelOrderService();
            var orders = TestOrderFactory.CreateTestOrders();
            var tempFolder = Path.Combine(Path.GetTempPath(), $"order_export_folder_{Guid.NewGuid():N}");

            try
            {
                var files = excelService.ExportOrdersToIndividualFiles(orders, tempFolder);
                Assert.Equal(orders.Count, files.Count);

                foreach (var file in files)
                {
                    Assert.True(File.Exists(file));
                    using var workbook = new XLWorkbook(file);
                    var ws = workbook.Worksheets.FirstOrDefault();
                    Assert.NotNull(ws);
                    Assert.Equal("ANDERSON'S BAKERY - PRODUCTION ORDER", ws.Cell(1, 1).GetString());
                }
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }

        [Fact]
        public void OrderSelectionItem_CorrectlyFormatsDetailsAndToggles()
        {
            var order = TestOrderFactory.CreateTestOrders().First(o => o.Customer == "Spar");
            var item = new OrderSelectionItem
            {
                Order = order,
                IsSelected = true
            };

            Assert.True(item.IsSelected);
            Assert.Equal(order.OrderId, item.Number);
            Assert.Equal("Spar", item.Customer);
            Assert.Equal("Complete", item.Status);
            Assert.True(item.IsComplete);
            Assert.Contains("Hamburger Rolls", item.ItemsSummary);

            // Toggle selection
            item.IsSelected = false;
            Assert.False(item.IsSelected);
        }

        [Fact]
        public void OrderService_BuildScalingSheetEmail_IncludesFullProductionAndMaterialDetails()
        {
            var orderService = new OrderService();
            var sparOrder = TestOrderFactory.CreateTestOrders().First(o => o.Customer == "Spar");

            var email = orderService.BuildScalingSheetEmail(sparOrder);
            Assert.NotNull(email);
            Assert.Contains("Spar", email.Subject);
            Assert.Contains("Hamburger Rolls", email.Body);
            Assert.Contains("Flour", email.Body);
            Assert.Contains("RAW MATERIAL SCALING", email.Body);
        }
    }
}

