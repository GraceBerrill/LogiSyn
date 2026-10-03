using System;
using System.Collections.Generic;
using System.Linq;
using LogiSyn.Interface;
using SharedLibrary.Model;

namespace LogiSyn.Services
{
    public class TempRecipeService : ITempRecipeService
    {
        private readonly List<ProductRecipeModel> _recipes = new()
        {
            new ProductRecipeModel
            {
                MatchPattern = "hamburger roll",
                DisplayName = "Hamburger Rolls",
                ProductionArea = "Production 1",
                UnitsPerPan = 125,
                PansPerTrolley = 2,
                Ingredients = new()
                {
                    new() { Name = "Flour", AmountPerUnit = 0.020, AdditionalRatio = 0.024, DefaultUsedRatio = 0.024, Unit = "bags" },
                    new() { Name = "Eggs", AmountPerUnit = 0.080, AdditionalRatio = 0.084, DefaultUsedRatio = 0.108, Unit = "dozen" },
                    new() { Name = "Salt", AmountPerUnit = 0.016, AdditionalRatio = 0.020, DefaultUsedRatio = 0.016, Unit = "bags" },
                    new() { Name = "Butter", AmountPerUnit = 0.016, AdditionalRatio = 0.020, DefaultUsedRatio = 0.008, Unit = "bags" }
                }
            },

            new ProductRecipeModel
            {
                MatchPattern = "croissant",
                DisplayName = "Croissants",
                ProductionArea = "Croissant Room",
                UnitsPerPan = 55,
                PansPerTrolley = 2,
                Ingredients = new()
                {
                    new() { Name = "Flour", AmountPerUnit = 0.045, AdditionalRatio = 0.054, DefaultUsedRatio = 0.054, Unit = "bags" },
                    new() { Name = "Eggs", AmountPerUnit = 0.181, AdditionalRatio = 0.190, DefaultUsedRatio = 0.245, Unit = "dozen" },
                    new() { Name = "Salt", AmountPerUnit = 0.036, AdditionalRatio = 0.045, DefaultUsedRatio = 0.036, Unit = "bags" }
                }
            },

            new ProductRecipeModel
            {
                MatchPattern = "roll",
                DisplayName = "Rolls",
                ProductionArea = "Production 3",
                UnitsPerPan = 50,
                PansPerTrolley = 2,
                Ingredients = new()
                {
                    new() { Name = "Flour", AmountPerUnit = 0.060, AdditionalRatio = 0.060, DefaultUsedRatio = 0.060, Unit = "bags" },
                    new() { Name = "Eggs", AmountPerUnit = 0.270, AdditionalRatio = 0.270, DefaultUsedRatio = 0.270, Unit = "dozen" },
                    new() { Name = "Salt", AmountPerUnit = 0.030, AdditionalRatio = 0.030, DefaultUsedRatio = 0.030, Unit = "bags" }
                }
            },

            new ProductRecipeModel
            {
                MatchPattern = "panini",
                DisplayName = "White Panini",
                ProductionArea = "Production 1",
                UnitsPerPan = 45,
                PansPerTrolley = 2,
                Ingredients = new()
                {
                    new() { Name = "Flour", AmountPerUnit = 0.022, AdditionalRatio = 0.025, DefaultUsedRatio = 0.022, Unit = "bags" },
                    new() { Name = "Salt", AmountPerUnit = 0.010, AdditionalRatio = 0.012, DefaultUsedRatio = 0.010, Unit = "bags" }
                }
            },

            new ProductRecipeModel
            {
                MatchPattern = "chelsea bun",
                DisplayName = "Chelsea Bun",
                ProductionArea = "Production 2",
                UnitsPerPan = 30,
                PansPerTrolley = 2,
                Ingredients = new()
                {
                    new() { Name = "Flour", AmountPerUnit = 0.035, AdditionalRatio = 0.040, DefaultUsedRatio = 0.035, Unit = "bags" },
                    new() { Name = "Butter", AmountPerUnit = 0.020, AdditionalRatio = 0.025, DefaultUsedRatio = 0.020, Unit = "bags" }
                }
            }
        };

        public ProductRecipeModel? FindRecipeByProductName(string productName)
        {
            return _recipes.FirstOrDefault(r =>
                productName.IndexOf(r.MatchPattern, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public IEnumerable<ProductRecipeModel> GetAllRecipes() => _recipes;
    }
}
