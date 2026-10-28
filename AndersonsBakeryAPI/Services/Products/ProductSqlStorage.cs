using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    // SQL Server data access storage helper for bakery product persistence
    public class ProductSqlStorage
    {
        private readonly ILogger? _logger;

        public ProductSqlStorage(ILogger? logger = null)
        {
            _logger = logger;
        }

        // Resolves SQL Server connection string from environment variables or falls back to localdb
        public string GetConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            if (!string.IsNullOrEmpty(env))
                return env;

            var alt1 = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings:SqlServer");
            if (!string.IsNullOrEmpty(alt1))
                return alt1;

            var alt2 = Environment.GetEnvironmentVariable("SqlServer")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");
            if (!string.IsNullOrEmpty(alt2))
                return alt2;

            return @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;TrustServerCertificate=True;";
        }

        // Loads all products from SQL Server database
        public List<Product> LoadProductsFromSql()
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand("SELECT ProductID, ProductName, PricePerUnit, SellBy, BestBefore, StorageLocation, Method, Ingredients FROM Product", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                var sqlProducts = new List<Product>();
                while (reader.Read())
                {
                    string rawIngredients = reader["Ingredients"] as string ?? string.Empty;
                    List<IngredientRequirement>? ingredients = null;
                    if (!string.IsNullOrWhiteSpace(rawIngredients))
                    {
                        try { ingredients = JsonSerializer.Deserialize<List<IngredientRequirement>>(rawIngredients); }
                        catch { }
                    }

                    sqlProducts.Add(new Product
                    {
                        ProductID = reader["ProductID"] != DBNull.Value ? Convert.ToInt32(reader["ProductID"]) : 0,
                        ProductName = reader["ProductName"] as string ?? string.Empty,
                        PricePerUnit = reader["PricePerUnit"] != DBNull.Value ? Convert.ToDecimal(reader["PricePerUnit"]) : 0m,
                        SellBy = reader["SellBy"] != DBNull.Value ? Convert.ToInt32(reader["SellBy"]) : 0,
                        BestBefore = reader["BestBefore"] != DBNull.Value ? Convert.ToInt32(reader["BestBefore"]) : 0,
                        StorageLocation = reader["StorageLocation"] as string ?? string.Empty,
                        Method = reader["Method"] as string ?? string.Empty,
                        Ingredients = ingredients ?? new List<IngredientRequirement>()
                    });
                }
                return sqlProducts;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error loading products from SQL");
                return new List<Product>();
            }
        }

        // Inserts a product record into SQL Server and sets the generated ProductID
        public void AddProduct(Product product)
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand(
                    "INSERT INTO Product (ProductName, PricePerUnit, SellBy, BestBefore, StorageLocation, Method, Ingredients) " +
                    "OUTPUT INSERTED.ProductID " +
                    "VALUES (@ProductName, @PricePerUnit, @SellBy, @BestBefore, @StorageLocation, @Method, @Ingredients)", conn);
                cmd.Parameters.AddWithValue("@ProductName", product.ProductName ?? string.Empty);
                cmd.Parameters.AddWithValue("@PricePerUnit", product.PricePerUnit);
                cmd.Parameters.AddWithValue("@SellBy", product.SellBy);
                cmd.Parameters.AddWithValue("@BestBefore", product.BestBefore);
                cmd.Parameters.AddWithValue("@StorageLocation", product.StorageLocation ?? string.Empty);
                cmd.Parameters.AddWithValue("@Method", product.Method ?? string.Empty);
                cmd.Parameters.AddWithValue("@Ingredients",
                    product.Ingredients != null && product.Ingredients.Count > 0
                        ? JsonSerializer.Serialize(product.Ingredients)
                        : (object)DBNull.Value);
                conn.Open();
                var insertedId = cmd.ExecuteScalar();
                if (insertedId != null && insertedId != DBNull.Value)
                {
                    product.ProductID = Convert.ToInt32(insertedId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding product to SQL");
            }
        }

        // Updates an existing product in SQL Server by ProductID or ProductName
        public void UpdateProduct(Product product)
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand(
                    "UPDATE Product SET PricePerUnit=@PricePerUnit, SellBy=@SellBy, BestBefore=@BestBefore, StorageLocation=@StorageLocation, Method=@Method, Ingredients=@Ingredients " +
                    "WHERE (ProductID > 0 AND ProductID = @ProductID) OR ProductName = @ProductName", conn);
                cmd.Parameters.AddWithValue("@ProductID", product.ProductID);
                cmd.Parameters.AddWithValue("@ProductName", product.ProductName ?? string.Empty);
                cmd.Parameters.AddWithValue("@PricePerUnit", product.PricePerUnit);
                cmd.Parameters.AddWithValue("@SellBy", product.SellBy);
                cmd.Parameters.AddWithValue("@BestBefore", product.BestBefore);
                cmd.Parameters.AddWithValue("@StorageLocation", product.StorageLocation ?? string.Empty);
                cmd.Parameters.AddWithValue("@Method", product.Method ?? string.Empty);
                cmd.Parameters.AddWithValue("@Ingredients",
                    product.Ingredients != null && product.Ingredients.Count > 0
                        ? JsonSerializer.Serialize(product.Ingredients)
                        : (object)DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating product in SQL");
            }
        }

        // Retrieves a single product from SQL Server by name or id
        public Product? GetProductByName(string name)
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                string sql = "SELECT ProductID, ProductName, PricePerUnit, SellBy, BestBefore, StorageLocation, Method, Ingredients FROM Product WHERE ProductName = @ProductName";
                if (int.TryParse(name, out int parsedId) && parsedId > 0)
                {
                    sql = "SELECT ProductID, ProductName, PricePerUnit, SellBy, BestBefore, StorageLocation, Method, Ingredients FROM Product WHERE ProductName = @ProductName OR ProductID = @ProductID";
                }

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProductName", name);
                if (int.TryParse(name, out parsedId) && parsedId > 0)
                {
                    cmd.Parameters.AddWithValue("@ProductID", parsedId);
                }

                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string rawIngredients = reader["Ingredients"] as string ?? string.Empty;
                    List<IngredientRequirement>? ingredients = null;
                    if (!string.IsNullOrWhiteSpace(rawIngredients))
                    {
                        try { ingredients = JsonSerializer.Deserialize<List<IngredientRequirement>>(rawIngredients); }
                        catch { }
                    }

                    return new Product
                    {
                        ProductID = reader["ProductID"] != DBNull.Value ? Convert.ToInt32(reader["ProductID"]) : 0,
                        ProductName = reader["ProductName"] as string ?? string.Empty,
                        PricePerUnit = reader["PricePerUnit"] != DBNull.Value ? Convert.ToDecimal(reader["PricePerUnit"]) : 0m,
                        SellBy = reader["SellBy"] != DBNull.Value ? Convert.ToInt32(reader["SellBy"]) : 0,
                        BestBefore = reader["BestBefore"] != DBNull.Value ? Convert.ToInt32(reader["BestBefore"]) : 0,
                        StorageLocation = reader["StorageLocation"] as string ?? string.Empty,
                        Method = reader["Method"] as string ?? string.Empty,
                        Ingredients = ingredients ?? new List<IngredientRequirement>()
                    };
                }
            }
            catch { }
            return null;
        }

        // Deletes a product from SQL Server by name or id
        public void DeleteByName(string name)
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                string sql = "DELETE FROM Product WHERE ProductName = @ProductName";
                if (int.TryParse(name, out int parsedId) && parsedId > 0)
                {
                    sql = "DELETE FROM Product WHERE ProductName = @ProductName OR ProductID = @ProductID";
                }

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProductName", name);
                if (int.TryParse(name, out parsedId) && parsedId > 0)
                {
                    cmd.Parameters.AddWithValue("@ProductID", parsedId);
                }

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting product from SQL");
            }
        }
    }
}
