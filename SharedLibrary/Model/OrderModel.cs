using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

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
        public int Amount { get; set; } = 100;
        public string ProductionLine { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public List<Ingredients> ReqIngredients { get; set; } = new();

        public Packaging Packaging { get; set; } = new();

        [JsonIgnore]
        public Packaging packaging
        {
            get => Packaging;
            set => Packaging = value;
        }
    }

    //------------------------------------------------------------------------------------------------//

    // Represents a raw material value with amount and unit, allowing implicit conversion to and from a tuple
    public class RawMaterialValue
    {
        public double Amount { get; set; }
        public string Unit { get; set; } = string.Empty;

        public RawMaterialValue() { }

        public RawMaterialValue(double amount, string unit)
        {
            Amount = amount;
            Unit = unit;
        }

        public static implicit operator RawMaterialValue((double Amount, string Unit) tuple)
            => new RawMaterialValue(tuple.Amount, tuple.Unit);

        public static implicit operator (double Amount, string Unit)(RawMaterialValue val)
            => (val.Amount, val.Unit);
    }

    //------------------------------------------------------------------------------------------------//

    // The complete order review showing the customer, the items ordered, and the raw materials used
    public class OrderScaled
    {
        public string OrderId { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public List<ProductionItem> ProductionItems { get; set; } = new();

        [JsonIgnore]
        public List<ProductionItem> productionItems
        {
            get => ProductionItems;
            set => ProductionItems = value;
        }

        public Dictionary<string, RawMaterialValue> RawMaterials { get; set; } = new();
    }

    //------------------------------------------------------------------------------------------------//

    // Email model
    public class EmailMessageModel
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}

//--------------------------------------End of File----------------------------------------------------------//
