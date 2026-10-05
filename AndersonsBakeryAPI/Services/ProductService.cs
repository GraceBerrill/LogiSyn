using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SharedLibrary.Interface;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class ProductService : IProductService
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

                if (sqlProducts.Count > 0)
                {
                    // Ensure any products with empty ingredients are populated from default catalog
                    var defaults = DefaultProductCatalog();
                    foreach (var p in sqlProducts)
                    {
                        if (p.Ingredients == null || p.Ingredients.Count == 0)
                        {
                            var match = defaults.FirstOrDefault(d => string.Equals(d.ProductName, p.ProductName, StringComparison.OrdinalIgnoreCase));
                            if (match != null && match.Ingredients.Count > 0)
                            {
                                p.Ingredients = new List<IngredientRequirement>(match.Ingredients);
                                if (string.IsNullOrWhiteSpace(p.Method)) p.Method = match.Method;
                            }
                        }
                    }
                    return sqlProducts;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error loading products from SQL: {ex.Message}");
            }

            var file = ProductsFilePath();
            if (File.Exists(file))
            {
                try
                {
                    var prodList = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file));
                    if (prodList != null && prodList.Count > 0)
                    {
                        var defaults = DefaultProductCatalog();
                        foreach (var p in prodList)
                        {
                            if (p.Ingredients == null || p.Ingredients.Count == 0)
                            {
                                var match = defaults.FirstOrDefault(d => string.Equals(d.ProductName, p.ProductName, StringComparison.OrdinalIgnoreCase));
                                if (match != null && match.Ingredients.Count > 0)
                                {
                                    p.Ingredients = new List<IngredientRequirement>(match.Ingredients);
                                    if (string.IsNullOrWhiteSpace(p.Method)) p.Method = match.Method;
                                }
                            }
                        }
                        return prodList;
                    }
                }
                catch { }

                try
                {
                    var rowList = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file));
                    if (rowList != null && rowList.Count > 0)
                    {
                        var defaults = DefaultProductCatalog();
                        return rowList.ConvertAll(r =>
                        {
                            var match = defaults.FirstOrDefault(d => string.Equals(d.ProductName, r.Name, StringComparison.OrdinalIgnoreCase));
                            return new Product
                            {
                                ProductName = r.Name,
                                PricePerUnit = ParsePrice(r.Price),
                                SellBy = ParseInt(r.SellBy),
                                BestBefore = ParseInt(r.BestBefore),
                                StorageLocation = r.Storage,
                                Method = match?.Method ?? string.Empty,
                                Ingredients = match?.Ingredients != null ? new List<IngredientRequirement>(match.Ingredients) : new List<IngredientRequirement>()
                            };
                        });
                    }
                }
                catch { }
            }

            var fallbackCatalog = DefaultProductCatalog();
            // Seed SQL and local JSON file with fallback catalog if empty
            try
            {
                File.WriteAllText(file, JsonSerializer.Serialize(fallbackCatalog, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }

            return fallbackCatalog;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error adding product to SQL: {ex.Message}");
            }

            // Always synchronize local JSON file as well so offline storage stays updated
            PersistProductToFile(product);
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

        private void PersistProductToFile(Product product)
        {
            try
            {
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

                var existingIdx = prodList.FindIndex(p => string.Equals(p.ProductName, product.ProductName, StringComparison.OrdinalIgnoreCase) || (product.ProductID > 0 && p.ProductID == product.ProductID));
                if (existingIdx >= 0)
                {
                    prodList[existingIdx] = product;
                }
                else
                {
                    if (product.ProductID <= 0)
                        product.ProductID = prodList.Count > 0 ? prodList[^1].ProductID + 1 : 1;
                    prodList.Add(product);
                }

                File.WriteAllText(file, JsonSerializer.Serialize(prodList, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error saving product to file: {ex.Message}");
            }
        }

        private static List<Product> DefaultProductCatalog()
        {
            return new List<Product>
            {
                new Product
                {
                    ProductID = 1,
                    ProductName = "Hamburger Rolls",
                    PricePerUnit = 24.99m,
                    SellBy = 5,
                    BestBefore = 5,
                    StorageLocation = "Freezer",
                    Method = "Mix dough for 10 minutes. Shape into rolls. Prove at 35C for 45 minutes. Bake at 200C for 15 minutes.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.020m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Eggs", Quantity = 0.080m, Unit = "dozen" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.016m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Butter", Quantity = 0.016m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 2,
                    ProductName = "Hotdog Rolls",
                    PricePerUnit = 24.99m,
                    SellBy = 6,
                    BestBefore = 6,
                    StorageLocation = "Freezer",
                    Method = "Mix ingredients until smooth. Proof for 40 minutes. Bake at 190C for 18 minutes.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.060m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Eggs", Quantity = 0.270m, Unit = "dozen" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.030m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 3,
                    ProductName = "Croissants",
                    PricePerUnit = 32.50m,
                    SellBy = 4,
                    BestBefore = 4,
                    StorageLocation = "Ambient",
                    Method = "Laminate butter into dough. Rest overnight. Shape and bake at 210C for 20 minutes.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.045m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Eggs", Quantity = 0.181m, Unit = "dozen" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.036m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 4,
                    ProductName = "White Panini",
                    PricePerUnit = 28.00m,
                    SellBy = 5,
                    BestBefore = 5,
                    StorageLocation = "Ambient",
                    Method = "Knead dough, ferment for 2 hours, shape into paninis, grill or bake lightly.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.022m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.010m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 5,
                    ProductName = "Chelsea Bun",
                    PricePerUnit = 19.99m,
                    SellBy = 3,
                    BestBefore = 3,
                    StorageLocation = "Ambient",
                    Method = "Roll dough with currants and cinnamon sugar. Cut into slices, proof, and bake at 180C.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.035m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Butter", Quantity = 0.020m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 6,
                    ProductName = "Bread",
                    PricePerUnit = 18.50m,
                    SellBy = 4,
                    BestBefore = 4,
                    StorageLocation = "Ambient",
                    Method = "Knead flour, water, yeast, salt. Bulk ferment, shape loaves, final proof, bake at 220C.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Flour", Quantity = 0.050m, Unit = "bags" },
                        new IngredientRequirement { IngredientName = "Salt", Quantity = 0.015m, Unit = "bags" }
                    }
                },
                new Product
                {
                    ProductID = 7,
                    ProductName = "Apple Juice",
                    PricePerUnit = 22.00m,
                    SellBy = 14,
                    BestBefore = 30,
                    StorageLocation = "Chilled",
                    Method = "Press fresh apples, filter, blend and pasteurize before cold bottling.",
                    Ingredients = new List<IngredientRequirement>
                    {
                        new IngredientRequirement { IngredientName = "Apple Concentrate", Quantity = 1.0m, Unit = "kg" }
                    }
                }
            };
        }
    }
}
