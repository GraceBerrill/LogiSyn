// Adriaan
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SharedLibrary.Model
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
        public double Pans { get; set; }
        public double Trolleys { get; set; }
        public double PansUsed { get; set; }
        public double TrolleysUsed { get; set; }

        public Packaging() { }

        public Packaging(double pans, double trolleys, double pansUsed = 0, double trolleysUsed = 0)
        {
            Pans = pans;
            Trolleys = trolleys;
            PansUsed = pansUsed;
            TrolleysUsed = trolleysUsed;
        }

        public Packaging(int pans, int trolleys, int pansUsed = 0, int trolleysUsed = 0)
        {
            Pans = pans;
            Trolleys = trolleys;
            PansUsed = pansUsed;
            TrolleysUsed = trolleysUsed;
        }

        [JsonIgnore]
        public int IntPans
        {
            get => (int)Math.Round(Pans);
            set => Pans = value;
        }

        [JsonIgnore]
        public int IntTrolleys
        {
            get => (int)Math.Round(Trolleys);
            set => Trolleys = value;
        }

        [JsonIgnore]
        public int IntPansUsed
        {
            get => (int)Math.Round(PansUsed);
            set => PansUsed = value;
        }

        [JsonIgnore]
        public int IntTrolleysUsed
        {
            get => (int)Math.Round(TrolleysUsed);
            set => TrolleysUsed = value;
        }
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

        [JsonIgnore]
        public double DoubleAmount
        {
            get => Amount;
            set => Amount = (int)Math.Round(value);
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
