using System;
using System.Collections.Generic;
using System.Text;

/// <summary> 
/// This model represents the data that is collected and shown in the ordering process
/// </summary>
namespace LogiSyn.Model
{
    // The ingredients requred
    public class Ingredients
    {
        public string IngredientName { get; set; } = string.Empty;
        public double IngredientAmount { get; set; }
<<<<<<< HEAD:LogiSyn/Model/OrderModel.cs
=======
        public double AdditionsAmount { get; set; }
        public double AmountUsed { get; set; }
>>>>>>> main:SharedLibrary/Model/OrderModel.cs
        public string MeasuredIngredient { get; set; } = string.Empty;
    }

    //------------------------------------------------------------------------------------------------//

    // Packaging pans and trolleys of products
    public class Packaging
    {
        public int Pans { get; set; }
<<<<<<< HEAD:LogiSyn/Model/OrderModel.cs
        public int Trolleys { get; set; }
=======
        // Legacy spelling variations: keep canonical Trolleys and alias Trollies for views/services
        public int Trolleys { get; set; }
        public int Trollies
        {
            get => Trolleys;
            set => Trolleys = value;
        }
        public int PansUsed { get; set; }
        public int TrolliesUsed { get; set; }
>>>>>>> main:SharedLibrary/Model/OrderModel.cs
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
<<<<<<< HEAD:LogiSyn/Model/OrderModel.cs
=======
        public string Status { get; set; } = string.Empty;
>>>>>>> main:SharedLibrary/Model/OrderModel.cs
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public List<ProductionItem> productionItems { get; set; } = new();

        public Dictionary<string, (double Amount, string Unit)> RawMaterials { get; set; } = new();
    }

}

//--------------------------------------End of File----------------------------------------------------------//
