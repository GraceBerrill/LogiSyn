using System;
using System.Collections.Generic;
using System.Text;

namespace LogiSyn.Model
{
    public class RecipeIngredientModel
    {
        public string Name { get; set; } = string.Empty;
        public double AmountPerUnit { get; set; }      
        public double AdditionalRatio { get; set; }    
        public double DefaultUsedRatio { get; set; }   
        public string Unit { get; set; } = string.Empty; 
    }

    public class ProductRecipeModel
    {
        public string MatchPattern { get; set; } = string.Empty; 
        public string DisplayName { get; set; } = string.Empty; 
        public string ProductionArea { get; set; } = "Production 1";
        public int UnitsPerPan { get; set; } = 125;
        public int PansPerTrolley { get; set; } = 2;
        public List<RecipeIngredientModel> Ingredients { get; set; } = new();
    }
}
