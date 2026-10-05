using System;
using System.Collections.Generic;
using SharedLibrary.Model;

namespace LogiSyn.Tests
{
    public static class TestOrderFactory
    {
        public static List<OrderScaled> CreateTestOrders()
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
                        }
                    },
                    RawMaterials = new Dictionary<string, RawMaterialValue>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Bread Flour", new RawMaterialValue(25, "kg") },
                        { "Water", new RawMaterialValue(18, "L") }
                    }
                }
            };
        }
    }
}

