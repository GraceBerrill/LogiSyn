using System.Collections.Generic;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    // Seed and default catalog definitions for bakery products, recipes, and shelf life
    public static class ProductCatalogDefaults
    {
        // Returns a predefined list of default products with recipe methods and ingredient requirements
        public static List<Product> DefaultProductCatalog()
        {
            return new List<Product>
            {
                new Product
                {
                    ProductID = 1,
                    ProductName = "Hamburger Rolls",
                    PricePerUnit = 24.99m,
                    SellBy = 5,
                    BestBefore = 5,
                    StorageLocation = "Freezer",
                    Method = "Mix dough for 10 minutes. Shape into rolls. Prove at 35C for 45 minutes. Bake at 200C for 15 minutes.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.020m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Eggs", Quantity = 0.080m, Unit = "dozen" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.016m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Butter", Quantity = 0.016m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 2,
                    ProductName = "Hotdog Rolls",
                    PricePerUnit = 24.99m,
                    SellBy = 6,
                    BestBefore = 6,
                    StorageLocation = "Freezer",
                    Method = "Mix ingredients until smooth. Proof for 40 minutes. Bake at 190C for 18 minutes.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.060m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Eggs", Quantity = 0.270m, Unit = "dozen" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.030m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 3,
                    ProductName = "Croissants",
                    PricePerUnit = 32.50m,
                    SellBy = 4,
                    BestBefore = 4,
                    StorageLocation = "Ambient",
                    Method = "Laminate butter into dough. Rest overnight. Shape and bake at 210C for 20 minutes.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.045m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Eggs", Quantity = 0.181m, Unit = "dozen" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.036m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 4,
                    ProductName = "White Panini",
                    PricePerUnit = 28.00m,
                    SellBy = 5,
                    BestBefore = 5,
                    StorageLocation = "Ambient",
                    Method = "Knead dough, ferment for 2 hours, shape into paninis, grill or bake lightly.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.022m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.010m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 5,
                    ProductName = "Chelsea Bun",
                    PricePerUnit = 19.99m,
                    SellBy = 3,
                    BestBefore = 3,
                    StorageLocation = "Ambient",
                    Method = "Roll dough with currants and cinnamon sugar. Cut into slices, proof, and bake at 180C.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.035m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Butter", Quantity = 0.020m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 6,
                    ProductName = "Bread",
                    PricePerUnit = 18.50m,
                    SellBy = 4,
                    BestBefore = 4,
                    StorageLocation = "Ambient",
                    Method = "Knead flour, water, yeast, salt. Bulk ferment, shape loaves, final proof, bake at 220C.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.050m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.015m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 7,
                    ProductName = "Apple Juice",
                    PricePerUnit = 22.00m,
                    SellBy = 14,
                    BestBefore = 30,
                    StorageLocation = "Chilled",
                    Method = "Press fresh apples, filter, blend and pasteurize before cold bottling.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Apple Concentrate", Quantity = 1.0m, Unit = "kg" }
                    }
                }
            };
        }

        // Returns fallback sample display rows
        public static List<ProductRow> SampleDefaults()
        {
            return new List<ProductRow>
            {
                new ProductRow { Name = "Hamburger Rolls", Price = "R24.99", SellBy = "5", BestBefore = "5", Storage = "Freezer" },
                new ProductRow { Name = "Hotdog Rolls", Price = "R24.99", SellBy = "6", BestBefore = "6", Storage = "Freezer" }
            };
        }
    }
}
