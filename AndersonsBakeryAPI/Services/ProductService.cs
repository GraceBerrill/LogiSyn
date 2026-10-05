using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using SharedLibrary.Interface;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Repositories;
using Microsoft.Extensions.Logging;

namespace AndersonsBakeryAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly MongoProductRepository? _mongoRepository;
        private readonly ILogger<ProductService>? _logger;

        public ProductService() : this(CreateDefaultMongoRepository()) { }

        public ProductService(MongoProductRepository? mongoRepository, ILogger<ProductService>? logger = null)
        {
            _mongoRepository = mongoRepository;
            _logger = logger;
        }

        private static MongoProductRepository? CreateDefaultMongoRepository()
        {
            try
            {
                string? conn = MongoConfiguration.TryGetConnectionString();
                if (string.IsNullOrWhiteSpace(conn)) return null;

                string dbName = MongoConfiguration.GetDatabaseName();
                return new MongoProductRepository(conn, dbName);
            }
            catch (Exception)
            {
                // Initialization failed; fallback to SQL/local storage. Logging is not available in static context.
                return null;
            }
        }

        private string GetConnectionString()
        {
            // Priority: explicit env var, then common environment keys used for app configuration,
            // then fall back to a sensible localdb default. This helps both the API and the WPF client
            // find the same SQL Server when running locally.
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            if (!string.IsNullOrEmpty(env))
                return env;

            // Support container/hosting style env var names that map to Configuration: ConnectionStrings:SqlServer
            var alt1 = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings:SqlServer");
            if (!string.IsNullOrEmpty(alt1))
                return alt1;

            var alt2 = Environment.GetEnvironmentVariable("SqlServer")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");
            if (!string.IsNullOrEmpty(alt2))
                return alt2;

            // Default to a localdb instance (include TrustServerCertificate to avoid cert issues on dev machines)
            return @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;TrustServerCertificate=True;";
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
            // 1. Try to load products from Mongo
            if (_mongoRepository != null)
            {
                try
                {
                    var mongoProducts = _mongoRepository.GetAllProducts();
                    if (mongoProducts != null && mongoProducts.Count > 0)
                    {
                        EnsureIngredientsFromCatalog(mongoProducts);
                        return mongoProducts;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error loading products from Mongo; falling back to SQL/local.");
                }
            }

            // 2. Try to load products from SQL
            var sqlProducts = LoadProductsFromSql();
            if (sqlProducts != null && sqlProducts.Count > 0)
            {
                EnsureIngredientsFromCatalog(sqlProducts);
                SeedMongoProductsIfEmpty(sqlProducts);
                return sqlProducts;
            }

            // 3. Fallback to local JSON file
            var file = ProductsFilePath();
            if (File.Exists(file))
            {
                try
                {
                    var prodList = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file));
                    if (prodList != null && prodList.Count > 0)
                    {
                        EnsureIngredientsFromCatalog(prodList);
                        SeedMongoProductsIfEmpty(prodList);
                        return prodList;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to read products from local JSON file.");
                }

                try
                {
                    var rowList = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file));
                    if (rowList != null && rowList.Count > 0)
                    {
                        var defaults = DefaultProductCatalog();
                        var converted = rowList.ConvertAll(r =>
                        {
                            var match = defaults.FirstOrDefault(d => string.Equals(d.ProductName, r.Name, StringComparison.OrdinalIgnoreCase));
                            return new Product
                            {
                                ProductID = r.ProductId,
                                ProductName = r.Name ?? string.Empty,
                                PricePerUnit = ParsePrice(r.Price ?? string.Empty),
                                SellBy = ParseInt(r.SellBy ?? string.Empty),
                                BestBefore = ParseInt(r.BestBefore ?? string.Empty),
                                StorageLocation = r.Storage ?? string.Empty,
                                Method = match?.Method ?? string.Empty,
                                Ingredients = match?.Ingredients != null ? new List<IngredientRequirement>(match.Ingredients) : new List<IngredientRequirement>()
                            };
                        });
                        SeedMongoProductsIfEmpty(converted);
                        return converted;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse legacy product rows from local JSON file.");
                }
            }

            // 4. Default fallback catalog
            var fallbackCatalog = DefaultProductCatalog();
            try
            {
                File.WriteAllText(file, JsonSerializer.Serialize(fallbackCatalog, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write fallback catalog to local JSON.");
            }

            SeedMongoProductsIfEmpty(fallbackCatalog);
            return fallbackCatalog;
        }

        // Get product rows for UI: calls GetAllProducts() so Mongo/SQL/JSON consistency is maintained
        public List<ProductRow> GetAll()
        {
            try
            {
                var products = GetAllProducts();
                if (products != null && products.Count > 0)
                {
                    return products.ConvertAll(p => new ProductRow
                    {
                        Id = p.Id,
                        ProductId = p.ProductID,
                        Name = p.ProductName ?? string.Empty,
                        Price = p.PricePerUnit > 0 ? ("R" + p.PricePerUnit.ToString("0.00")) : string.Empty,
                        SellBy = p.SellBy.ToString(),
                        BestBefore = p.BestBefore.ToString(),
                        Storage = p.StorageLocation ?? string.Empty
                    });
                }
            }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error in GetAll when loading products from Mongo");
                }

            return SampleDefaults();
        }

        // Add a full Product model to Mongo, SQL, and JSON
        public void Add(Product product)
        {
            if (product == null) return;

            // 1. Save to MongoDB
            if (_mongoRepository != null)
            {
                try
                {
                    _mongoRepository.Upsert(product);
                }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error adding product to Mongo");
                    }
            }

            // 2. Save to SQL
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
                    // Update Mongo with the generated SQL ProductID
                    if (_mongoRepository != null)
                    {
                        try { _mongoRepository.Update(product); }
                        catch { }
                    }
                }
            }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error adding product to SQL");
                }

            // 3. Always synchronize local JSON file
            PersistProductToFile(product);
        }

        // Add using older ProductRow (keeps backward compatibility)
        public void Add(ProductRow row)
        {
            if (row == null) return;

            var newProd = new Product
            {
                ProductID = row.ProductId,
                ProductName = row.Name ?? string.Empty,
                PricePerUnit = ParsePrice(row.Price ?? string.Empty),
                SellBy = ParseInt(row.SellBy ?? string.Empty),
                BestBefore = ParseInt(row.BestBefore ?? string.Empty),
                StorageLocation = row.Storage ?? string.Empty,
                Method = string.Empty,
                Ingredients = new List<IngredientRequirement>()
            };

            Add(newProd);
        }

        // Update existing product by name/id across Mongo, SQL, and JSON
        public void UpdateFromDetail(Product product)
        {
            if (product == null || string.IsNullOrEmpty(product.ProductName)) return;

            // 1. Update in MongoDB
            if (_mongoRepository != null)
            {
                try
                {
                    _mongoRepository.Upsert(product);
                }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error updating product in Mongo");
                    }
            }

            // 2. Update in SQL Server
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

            // 3. Update in local JSON file
            try
            {
                var file = ProductsFilePath();
                if (File.Exists(file))
                {
                    var list = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file)) ?? new List<Product>();
                    var idx = list.FindIndex(p => string.Equals(p.ProductName, product.ProductName, StringComparison.OrdinalIgnoreCase)
                                               || (product.ProductID > 0 && p.ProductID == product.ProductID));
                    if (idx >= 0)
                    {
                        if (product.ProductID <= 0) product.ProductID = list[idx].ProductID;
                        list[idx] = product;
                        File.WriteAllText(file, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else
                    {
                        list.Add(product);
                        File.WriteAllText(file, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
            }
            catch { }
        }

        // Get single product by name or id
        public Product? GetProductByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            // 1. Try MongoDB
            if (_mongoRepository != null)
            {
                try
                {
                    var prod = _mongoRepository.GetByName(name);
                    if (prod != null) return prod;

                    if (int.TryParse(name, out int id) && id > 0)
                    {
                        prod = _mongoRepository.GetByProductId(id);
                        if (prod != null) return prod;
                    }
                }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error getting product from Mongo");
                    }
            }

            // 2. Try SQL
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

            // 3. Try JSON file
            var file = ProductsFilePath();
            if (File.Exists(file))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    try
                    {
                        var list = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                        var prod = list.Find(p => string.Equals(p.ProductName, name, StringComparison.OrdinalIgnoreCase)
                                               || (int.TryParse(name, out int pid) && p.ProductID == pid));
                        if (prod != null) return prod;
                    }
                    catch { }

                    try
                    {
                        var rows = JsonSerializer.Deserialize<List<ProductRow>>(json) ?? new List<ProductRow>();
                        var row = rows.Find(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)
                                              || (int.TryParse(name, out int rid) && r.ProductId == rid));
                        if (row != null)
                        {
                            return new Product
                            {
                                ProductID = row.ProductId,
                                ProductName = row.Name ?? string.Empty,
                                PricePerUnit = ParsePrice(row.Price ?? string.Empty),
                                SellBy = ParseInt(row.SellBy ?? string.Empty),
                                BestBefore = ParseInt(row.BestBefore ?? string.Empty),
                                StorageLocation = row.Storage ?? string.Empty
                            };
                        }
                    }
                    catch { }
                }
                catch { }
            }

            return null;
        }

        // Delete by name or id across Mongo, SQL, and JSON
        public void DeleteByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            // 1. Delete from MongoDB
            if (_mongoRepository != null)
            {
                try
                {
                    _mongoRepository.DeleteByName(name);
                    if (int.TryParse(name, out int parsedId) && parsedId > 0)
                    {
                        _mongoRepository.DeleteByProductId(parsedId);
                    }
                }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error deleting product from Mongo");
                    }
            }

            // 2. Delete from SQL Server
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

            // 3. Delete from local JSON file
            var file = ProductsFilePath();
            if (File.Exists(file))
            {
                try
                {
                    int.TryParse(name, out int parsedId);
                    try
                    {
                        var list = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file)) ?? new List<Product>();
                        var filtered = list.Where(p => !string.Equals(p.ProductName, name, StringComparison.OrdinalIgnoreCase)
                                                    && !(parsedId > 0 && p.ProductID == parsedId)).ToList();
                        File.WriteAllText(file, JsonSerializer.Serialize(filtered, new JsonSerializerOptions { WriteIndented = true }));
                        return;
                    }
                    catch { }

                    var listRow = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file)) ?? new List<ProductRow>();
                    var filteredRow = listRow.Where(p => !string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
                                                      && !(parsedId > 0 && p.ProductId == parsedId)).ToList();
                    File.WriteAllText(file, JsonSerializer.Serialize(filteredRow, new JsonSerializerOptions { WriteIndented = true }));
                }
                catch { }
            }
        }

        private List<Product> LoadProductsFromSql()
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

        private static void EnsureIngredientsFromCatalog(List<Product> products)
        {
            var defaults = DefaultProductCatalog();
            foreach (var p in products)
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
        }

        private void SeedMongoProductsIfEmpty(IEnumerable<Product> products)
        {
            if (_mongoRepository != null)
            {
                try
                {
                    _mongoRepository.SeedIfEmpty(products);
                }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error seeding products to Mongo");
                    }
            }
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
                                ProductID = r.ProductId,
                                ProductName = r.Name ?? string.Empty,
                                PricePerUnit = ParsePrice(r.Price ?? string.Empty),
                                SellBy = ParseInt(r.SellBy ?? string.Empty),
                                BestBefore = ParseInt(r.BestBefore ?? string.Empty),
                                StorageLocation = r.Storage ?? string.Empty
                            });
                        }
                        catch { prodList = new List<Product>(); }
                    }
                }
                else prodList = new List<Product>();

                var existingIdx = prodList.FindIndex(p => string.Equals(p.ProductName, product.ProductName, StringComparison.OrdinalIgnoreCase)
                                                       || (product.ProductID > 0 && p.ProductID == product.ProductID));
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
                    _logger?.LogWarning(ex, "Error saving product to file");
                }
        }

        public static List<Product> DefaultProductCatalog()
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

