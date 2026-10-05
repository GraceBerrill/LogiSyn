using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class ProductService
    {
        private string GetConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            if (!string.IsNullOrEmpty(env))
                return env;
            return @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";
        }

        private string ProductsFilePath()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            return Path.Combine(dataFolder, "products.json");
        }

        // Get full Product models (with ingredients)
        public List<Product> GetAllProducts()
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
                if (sqlProducts.Count > 0) return sqlProducts;
            }
            catch { }

            var file = ProductsFilePath();
            if (File.Exists(file))
            {
                try
                {
                    var prodList = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file));
                    if (prodList != null && prodList.Count > 0)
                        return prodList;
                }
                catch { }

                try
                {
                    var rowList = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file));
                    if (rowList != null && rowList.Count > 0)
                    {
                        return rowList.ConvertAll(r => new Product
                        {
                            ProductName = r.Name,
                            PricePerUnit = ParsePrice(r.Price),
                            SellBy = ParseInt(r.SellBy),
                            BestBefore = ParseInt(r.BestBefore),
                            StorageLocation = r.Storage
                        });
                    }
                }
                catch { }
            }

            return GetAll().ConvertAll(r => new Product
            {
                ProductName = r.Name,
                PricePerUnit = ParsePrice(r.Price),
                SellBy = ParseInt(r.SellBy),
                BestBefore = ParseInt(r.BestBefore),
                StorageLocation = r.Storage
            });
        }

        // Get product rows for UI: prefer SQL, fall back to JSON. JSON may be either List<Product> or legacy List<ProductRow>.
        public List<ProductRow> GetAll()
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand("SELECT ProductID, ProductName, PricePerUnit, SellBy, BestBefore, StorageLocation FROM Product", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                var list = new List<ProductRow>();
                while (reader.Read())
                {
                    decimal price = reader["PricePerUnit"] != DBNull.Value ? Convert.ToDecimal(reader["PricePerUnit"]) : 0m;
                    list.Add(new ProductRow
                    {
                        Name = reader["ProductName"] as string ?? string.Empty,
                        Price = price > 0 ? ("R" + price.ToString("0.00")) : string.Empty,
                        SellBy = reader["SellBy"] != DBNull.Value ? reader["SellBy"].ToString() ?? string.Empty : string.Empty,
                        BestBefore = reader["BestBefore"] != DBNull.Value ? reader["BestBefore"].ToString() ?? string.Empty : string.Empty,
                        Storage = reader["StorageLocation"] as string ?? string.Empty
                    });
                }
                if (list.Count > 0) return list;
            }
            catch { }

            var file = ProductsFilePath();
            if (!File.Exists(file))
            {
                var defaults = SampleDefaults();
                File.WriteAllText(file, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(file);
                try
                {
                    var prodList = JsonSerializer.Deserialize<List<Product>>(json);
                    if (prodList != null)
                    {
                        return prodList.ConvertAll(p => new ProductRow
                        {
                            Name = p.ProductName ?? string.Empty,
                            Price = p.PricePerUnit > 0 ? ("R" + p.PricePerUnit.ToString("0.00")) : string.Empty,
                            SellBy = p.SellBy.ToString(),
                            BestBefore = p.BestBefore.ToString(),
                            Storage = p.StorageLocation ?? string.Empty
                        });
                    }
                }
                catch { }

                var list = JsonSerializer.Deserialize<List<ProductRow>>(json);
                return list ?? SampleDefaults();
            }
            catch
            {
                var defaults = SampleDefaults();
                File.WriteAllText(file, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
                return defaults;
            }
        }

        // Add a full Product model
        public void Add(Product product)
        {
            if (product == null) return;
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
                    product.ProductID = Convert.ToInt32(insertedId);
                return;
            }
            catch { }

            var file = ProductsFilePath();
            List<Product> prodList;
            if (File.Exists(file))
            {
                try { prodList = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file)) ?? new List<Product>(); }
                catch
                {
                    try
                    {
                        var rows = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file)) ?? new List<ProductRow>();
                        prodList = rows.ConvertAll(r => new Product
                        {
                            ProductName = r.Name,
                            PricePerUnit = ParsePrice(r.Price),
                            SellBy = ParseInt(r.SellBy),
                            BestBefore = ParseInt(r.BestBefore),
                            StorageLocation = r.Storage
                        });
                    }
                    catch { prodList = new List<Product>(); }
                }
            }
            else prodList = new List<Product>();

            product.ProductID = prodList.Count > 0 ? prodList[^1].ProductID + 1 : 1;
            prodList.Add(product);
            File.WriteAllText(file, JsonSerializer.Serialize(prodList, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Add using older ProductRow (keeps backward compatibility)
        public void Add(ProductRow row)
        {
            if (row == null) return;
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand(
                    "INSERT INTO Product (ProductName, PricePerUnit, SellBy, BestBefore, StorageLocation) " +
                    "VALUES (@ProductName, @PricePerUnit, @SellBy, @BestBefore, @StorageLocation)", conn);
                cmd.Parameters.AddWithValue("@ProductName", row.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@PricePerUnit", ParsePrice(row.Price));
                cmd.Parameters.AddWithValue("@SellBy", ParseInt(row.SellBy));
                cmd.Parameters.AddWithValue("@BestBefore", ParseInt(row.BestBefore));
                cmd.Parameters.AddWithValue("@StorageLocation", row.Storage ?? string.Empty);
                conn.Open();
                cmd.ExecuteNonQuery();
                return;
            }
            catch { }

            var file = ProductsFilePath();
            List<Product> prodList;
            if (File.Exists(file))
            {
                try { prodList = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file)) ?? new List<Product>(); }
                catch
                {
                    try
                    {
                        var rows = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file)) ?? new List<ProductRow>();
                        prodList = rows.ConvertAll(r => new Product
                        {
                            ProductName = r.Name,
                            PricePerUnit = ParsePrice(r.Price),
                            SellBy = ParseInt(r.SellBy),
                            BestBefore = ParseInt(r.BestBefore),
                            StorageLocation = r.Storage
                        });
                    }
                    catch { prodList = new List<Product>(); }
                }
            }
            else prodList = new List<Product>();

            var newProd = new Product
            {
                ProductID = prodList.Count > 0 ? prodList[^1].ProductID + 1 : 1,
                ProductName = row.Name,
                PricePerUnit = ParsePrice(row.Price),
                SellBy = ParseInt(row.SellBy),
                BestBefore = ParseInt(row.BestBefore),
                StorageLocation = row.Storage,
                Method = string.Empty,
                Ingredients = new List<IngredientRequirement>()
            };

            prodList.Add(newProd);
            File.WriteAllText(file, JsonSerializer.Serialize(prodList, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Update existing product by name
        public void UpdateFromDetail(Product product)
        {
            if (product == null || string.IsNullOrEmpty(product.ProductName)) return;
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
                return;
            }
            catch { }

            var file = ProductsFilePath();
            if (!File.Exists(file)) return;
            try
            {
                var list = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file)) ?? new List<Product>();
                var idx = list.FindIndex(p => string.Equals(p.ProductName, product.ProductName, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    product.ProductID = list[idx].ProductID;
                    list[idx] = product;
                    File.WriteAllText(file, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch { }
        }

        // Get single product by name
        public Product? GetProductByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand(
                    "SELECT ProductID, ProductName, PricePerUnit, SellBy, BestBefore, StorageLocation, Method, Ingredients " +
                    "FROM Product WHERE ProductName = @ProductName", conn);
                cmd.Parameters.AddWithValue("@ProductName", name);
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

            var file = ProductsFilePath();
            if (!File.Exists(file)) return null;
            try
            {
                var json = File.ReadAllText(file);
                try
                {
                    var list = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                    var prod = list.Find(p => string.Equals(p.ProductName, name, StringComparison.OrdinalIgnoreCase));
                    if (prod != null) return prod;
                }
                catch { }

                try
                {
                    var rows = JsonSerializer.Deserialize<List<ProductRow>>(json) ?? new List<ProductRow>();
                    var row = rows.Find(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (row != null)
                    {
                        return new Product
                        {
                            ProductName = row.Name,
                            PricePerUnit = ParsePrice(row.Price),
                            SellBy = ParseInt(row.SellBy),
                            BestBefore = ParseInt(row.BestBefore),
                            StorageLocation = row.Storage
                        };
                    }
                }
                catch { }
            }
            catch { }
            return null;
        }

        // Delete by name (works with Product JSON or legacy ProductRow JSON)
        public void DeleteByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand("DELETE FROM Product WHERE ProductName = @ProductName", conn);
                cmd.Parameters.AddWithValue("@ProductName", name);
                conn.Open();
                cmd.ExecuteNonQuery();
                return;
            }
            catch { }

            var file = ProductsFilePath();
            if (!File.Exists(file)) return;
            try
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file)) ?? new List<Product>();
                    var filtered = list.Where(p => !string.Equals(p.ProductName, name, StringComparison.OrdinalIgnoreCase)).ToList();
                    File.WriteAllText(file, JsonSerializer.Serialize(filtered, new JsonSerializerOptions { WriteIndented = true }));
                    return;
                }
                catch { }

                var listRow = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file)) ?? new List<ProductRow>();
                var filteredRow = listRow.Where(p => !string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
                File.WriteAllText(file, JsonSerializer.Serialize(filteredRow, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static decimal ParsePrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price)) return 0m;
            var cleaned = price.Replace("R", "").Replace("$", "").Trim();
            if (decimal.TryParse(cleaned, out var v)) return v;
            return 0m;
        }

        private static int ParseInt(string s)
        {
            if (int.TryParse(s, out var v)) return v;
            return 0;
        }

        private List<ProductRow> SampleDefaults()
        {
            return new List<ProductRow>
            {
                new ProductRow { Name = "Hamburger Rolls", Price = "R24.99", SellBy = "5", BestBefore = "5", Storage = "Freezer" },
                new ProductRow { Name = "Hotdog Rolls", Price = "R24.99", SellBy = "6", BestBefore = "6", Storage = "Freezer" }
            };
        }
    }
}
