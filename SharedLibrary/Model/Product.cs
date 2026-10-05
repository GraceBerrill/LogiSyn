using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SharedLibrary.Model
{
    [BsonIgnoreExtraElements]
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [BsonElement("ProductID")]
        public int ProductID { get; set; }

        [BsonElement("ProductName")]
        public string ProductName { get; set; } = string.Empty;

        [BsonElement("PricePerUnit")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PricePerUnit { get; set; }

        [BsonElement("SellBy")]
        public int SellBy { get; set; }

        [BsonElement("BestBefore")]
        public int BestBefore { get; set; }

        //for Ambient, Chilled, Frozen
        [BsonElement("StorageLocation")]
        public string StorageLocation { get; set; } = "Ambient";

        [BsonElement("Method")]
        public string Method { get; set; } = string.Empty;

        [BsonElement("Ingredients")]
        public List<IngredientRequirement> Ingredients { get; set; } = new();
    }

    //required ingredients for a product
    /********************************************************************************************/
    [BsonIgnoreExtraElements]
    public class IngredientRequirement
    {
        [BsonElement("IngredientName")]
        public string IngredientName { get; set; } = string.Empty;

        [BsonElement("Quantity")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; }

        [BsonElement("Unit")]
        public string Unit { get; set; } = "kg";
    }
}
/*********************************************MAR26EOF*******************************************/