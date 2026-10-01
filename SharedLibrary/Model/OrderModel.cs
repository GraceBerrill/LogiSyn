using System;
using System.Collections.Generic;
using System.Text;

/// <summary> 
/// This model represents the data that is collected and shown in the ordering process
/// </summary>
namespace LogiSyn.Model
{
    public class Ingredients
    {
        public string IngredientName { get; set; } = string.Empty;
        public double IngredientAmount { get; set; }
        public double AdditionsAmount { get; set; }
        public double AmountUsed { get; set; }
        public string MeasuredIngredient { get; set; } = string.Empty;
    }

    //------------------------------------------------------------------------------------------------//

    // Packaging pans and trolleys of products
    public class Packaging
    {
        public int Pans { get; set; }
        public int Trolleys { get; set; }
        public int PansUsed { get; set; }
        public int TrolleysUsed { get; set; }
    }

    //------------------------------------------------------------------------------------------------//

    // Each items individual details (production line, ingredients it requires, etc)
    public class ProductionItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public List<Ingredients> ReqIngredients { get; set; } = new();
        public Packaging packaging { get; set; } = new();
    }

    //------------------------------------------------------------------------------------------------//

    // The complete order review showing the customer, the items ordered, and the raw materials used
    public class OrderScaled
    {
        public string OrderId { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public List<ProductionItem> productionItems { get; set; } = new();

        public Dictionary<string, (double Amount, string Unit)> RawMaterials { get; set; } = new();
    }

}

//--------------------------------------End of File----------------------------------------------------------//
