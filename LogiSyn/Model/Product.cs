using System;
using System.Collections.Generic;
using System.Text;

namespace LogiSyn.Model
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public int SellBy { get; set; }
        public int BestBefore { get; set; }

        //for Ambient, Chilled, Frozen
        public string StorageLocation { get; set; } = "Ambient";
        public string Method { get; set; } = string.Empty;
        public List<IngredientRequirement> Ingredients { get; set; } = new();
    }

    //required ingredients for a product
    /********************************************************************************************/
    public class IngredientRequirement
    {
        public string IngredientName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "kg";
    }
}
/*********************************************MAR26EOF*******************************************/