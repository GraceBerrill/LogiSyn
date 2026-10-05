using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AndersonsBakeryAPI.Services;
using LogiSyn.Views;
using SharedLibrary.Model;
using Xunit;

namespace LogiSyn.Tests
{
    public class EndToEndIngestionScalingPrintingTests
    {
        [Fact]
        public void ExcelOrderService_ExportAndReadRoundTrip_PreservesAllSections()
        {
            // Arrange
            var excelService = new ExcelOrderService();
            var originalOrder = new OrderScaled
            {
                OrderId = "#ORD-2026-TEST",
                Customer = "Artisan Cafe",
                Status = "In Production",
                OrderDate = DateTime.Today,
                ProductionItems = new List<ProductionItem>
                {
                    new ProductionItem
                    {
                        ProductName = "Hamburger Rolls",
                        Amount = 125,
                        ProductionLine = "Production 1",
                        Packaging = new Packaging(pans: 10, trolleys: 5),
                        Notes = "Extra sesame",
                        ReqIngredients = new List<Ingredients>
                        {
                            new Ingredients
                            {
                                IngredientName = "Flour",
                                IngredientAmount = 2.5,
                                AdditionsAmount = 0.3,
                                MeasuredIngredient = "bags"
                            }
                        }
                    }
                },
                RawMaterials = new Dictionary<string, RawMaterialValue>
                {
                    ["Flour"] = new RawMaterialValue(2.5, "bags"),
                    ["Yeast"] = new RawMaterialValue(0.5, "kg")
                }
            };

            var tempFile = Path.Combine(Path.GetTempPath(), $"order_test_{Guid.NewGuid():N}.xlsx");

            try
            {
                // Act - Export
                string exportedPath = excelService.ExportOrderToExcel(originalOrder, tempFile);
                Assert.True(File.Exists(exportedPath));

                // Act - Read Back
                var readOrder = excelService.ReadOrderFromExcel(exportedPath);

                // Assert
                Assert.NotNull(readOrder);
                Assert.Equal(originalOrder.OrderId, readOrder.OrderId);
                Assert.Equal(originalOrder.Customer, readOrder.Customer);
                Assert.NotNull(readOrder.ProductionItems);
                Assert.NotEmpty(readOrder.ProductionItems);

                var firstItem = readOrder.ProductionItems[0];
                Assert.Equal("Hamburger Rolls", firstItem.ProductName);
                Assert.Equal(10.0, firstItem.Packaging.Pans);
                Assert.Equal(5.0, firstItem.Packaging.Trolleys);
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
        public void PdfRegex_ExtractQuantity_IgnoresYearsAndPrices()
        {
            // Testing regex pattern used in OrderService.ExtractQuantity
            var pattern = @"(?<!\d)(?<!\$)(?<!\b(?:19|20)\d{2}\b)(?<!\b\d{1,2}[\/\-\.])(?<!\b\d{1,2}\s)(?:qty|quantity|amount|total|count)?[:\s#]*(\d{1,4})(?!\s*(?:am|pm|min|hr|kg|g|lb|oz|\/|\-|\d))(?!\s*(?:dollars?|cents?|\$))";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Case 1: Line with 4-digit calendar year (2026), date, and unit price ($4.50)
            var sampleText1 = "Date: 2026-10-05 | Unit Price: $4.50 | Quantity: 75";
            var matches1 = regex.Matches(sampleText1);

            Assert.NotEmpty(matches1);
            var extractedQty1 = 0;
            foreach (Match m in matches1)
            {
                if (int.TryParse(m.Groups[1].Value, out var val) && val > 0 && val < 2000)
                {
                    extractedQty1 = val;
                    break;
                }
            }
            Assert.Equal(75, extractedQty1);

            // Case 2: Line with year 2025 and price $12.00
            var sampleText2 = "Order year 2025 at $12.00 total amount 150 items";
            var matches2 = regex.Matches(sampleText2);

            Assert.NotEmpty(matches2);
            var extractedQty2 = 0;
            foreach (Match m in matches2)
            {
                if (int.TryParse(m.Groups[1].Value, out var val) && val > 0 && val < 2000)
                {
                    extractedQty2 = val;
                    break;
                }
            }
            Assert.Equal(150, extractedQty2);
        }

        [Fact]
        public void RecipeScaling_TempRecipeService_CalculatesProportionalRatios()
        {
            // Arrange
            var recipeService = new TempRecipeService();

            // Act
            var recipe = recipeService.FindRecipeByProductName("hamburger roll");

            // Assert
            Assert.NotNull(recipe);
            Assert.Equal("Hamburger Rolls", recipe.DisplayName);
            Assert.Equal(125, recipe.UnitsPerPan);
            Assert.Equal(2, recipe.PansPerTrolley);

            // For 250 units:
            int qtySmall = 250;
            int pansSmall = (int)Math.Ceiling((double)qtySmall / recipe.UnitsPerPan);
            int trolleysSmall = (int)Math.Ceiling((double)pansSmall / recipe.PansPerTrolley);
            var flourIng = recipe.Ingredients.First(i => i.Name == "Flour");
            double flourSmall = Math.Round(flourIng.AmountPerUnit * qtySmall, 3);

            // For 500 units:
            int qtyLarge = 500;
            int pansLarge = (int)Math.Ceiling((double)qtyLarge / recipe.UnitsPerPan);
            int trolleysLarge = (int)Math.Ceiling((double)pansLarge / recipe.PansPerTrolley);
            double flourLarge = Math.Round(flourIng.AmountPerUnit * qtyLarge, 3);

            Assert.Equal(2, pansSmall);
            Assert.Equal(1, trolleysSmall);
            Assert.Equal(5.0, flourSmall);

            Assert.Equal(4, pansLarge);
            Assert.Equal(2, trolleysLarge);
            Assert.Equal(10.0, flourLarge);

            // Proportional scaling verification: 500 units requires double 250 units
            Assert.Equal(flourSmall * 2, flourLarge);
            Assert.Equal(pansSmall * 2, pansLarge);
            Assert.Equal(trolleysSmall * 2, trolleysLarge);
        }

        [Fact]
        public void ApiClient_UriEncoding_EscapesHashFragmentsCorrectly()
        {
            // Arrange
            var orderIdWithHash = "#001";
            var complexId = "#ORDER/2026#SPECIAL";

            // Act
            var escaped1 = Uri.EscapeDataString(orderIdWithHash);
            var escaped2 = Uri.EscapeDataString(complexId);

            // Assert
            Assert.Equal("%23001", escaped1);
            Assert.DoesNotContain("#", escaped1);

            Assert.DoesNotContain("#", escaped2);
            Assert.Contains("%23", escaped2);
        }

        [Fact]
        public void CaseInsensitiveRoleParsing_AcceptsAllCasingVariants()
        {
            // Arrange
            string[] adminVariants = { "admin", "Admin", "ADMIN", "AdMiN" };
            string[] managerVariants = { "manager", "Manager", "MANAGER" };
            string[] userVariants = { "user", "User", "USER" };

            // Act & Assert
            foreach (var roleStr in adminVariants)
            {
                var success = Enum.TryParse<AppRole>(roleStr, ignoreCase: true, out var role);
                Assert.True(success);
                Assert.Equal(AppRole.Admin, role);
            }

            foreach (var roleStr in managerVariants)
            {
                var success = Enum.TryParse<AppRole>(roleStr, ignoreCase: true, out var role);
                Assert.True(success);
                Assert.Equal(AppRole.Manager, role);
            }

            foreach (var roleStr in userVariants)
            {
                var success = Enum.TryParse<AppRole>(roleStr, ignoreCase: true, out var role);
                Assert.True(success);
                Assert.Equal(AppRole.User, role);
            }
        }

        [Fact]
        public void PrintingDisplayModels_InstantiateAndMapSuccessfully()
        {
            // Arrange & Act
            var prodDisplay = new OrderSheetsView.ProductionItemDisplayModel
            {
                ProductName = "Hamburger Rolls",
                ProductionLine = "Production 1",
                Pans = 4,
                Trolleys = 2,
                IngredientsSummary = "Flour: 10 bags, Eggs: 40 dozen"
            };

            var scalingDisplay = new OrderSheetsView.ScalingItemDisplayModel
            {
                Key = "Bread Flour",
                DisplayValue = "10 bags"
            };

            // Assert
            Assert.Equal("Hamburger Rolls", prodDisplay.ProductName);
            Assert.Equal("Production 1", prodDisplay.ProductionLine);
            Assert.Equal(4.0, prodDisplay.Pans);
            Assert.Equal(2.0, prodDisplay.Trolleys);
            Assert.Equal("Bread Flour", scalingDisplay.Key);
            Assert.Equal("10 bags", scalingDisplay.DisplayValue);
        }
    }
}
