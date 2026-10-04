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

        // Get product rows for UI: prefer SQL, fall back to JSON. JSON may be either List<Product> or legacy List<ProductRow>.
        public List<ProductRow> GetAll()
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand("SELECT Name, Price, SellBy, BestBefore, Storage FROM Product", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                var list = new List<ProductRow>();
                while (reader.Read())
                {
                    list.Add(new ProductRow
                    {
                        Name = reader["Name"] as string ?? string.Empty,
                        Price = reader["Price"] as string ?? string.Empty,
                        SellBy = reader["SellBy"] as string ?? string.Empty,
                        BestBefore = reader["BestBefore"] as string ?? string.Empty,
                        Storage = reader["Storage"] as string ?? string.Empty
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
                using var cmd = new SqlCommand("INSERT INTO Product (Name, Price, SellBy, BestBefore, Storage) VALUES (@Name,@Price,@SellBy,@BestBefore,@Storage)", conn);
                cmd.Parameters.AddWithValue("@Name", product.ProductName ?? string.Empty);
                cmd.Parameters.AddWithValue("@Price", product.PricePerUnit.ToString());
                cmd.Parameters.AddWithValue("@SellBy", product.SellBy.ToString());
                cmd.Parameters.AddWithValue("@BestBefore", product.BestBefore.ToString());
                cmd.Parameters.AddWithValue("@Storage", product.StorageLocation ?? string.Empty);
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
                using var cmd = new SqlCommand("INSERT INTO Product (Name, Price, SellBy, BestBefore, Storage) VALUES (@Name,@Price,@SellBy,@BestBefore,@Storage)", conn);
                cmd.Parameters.AddWithValue("@Name", row.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@Price", row.Price ?? string.Empty);
                cmd.Parameters.AddWithValue("@SellBy", row.SellBy ?? string.Empty);
                cmd.Parameters.AddWithValue("@BestBefore", row.BestBefore ?? string.Empty);
                cmd.Parameters.AddWithValue("@Storage", row.Storage ?? string.Empty);
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
                using var cmd = new SqlCommand("UPDATE Product SET Price=@Price, SellBy=@SellBy, BestBefore=@BestBefore, Storage=@Storage WHERE Name=@Name", conn);
                cmd.Parameters.AddWithValue("@Name", product.ProductName ?? string.Empty);
                cmd.Parameters.AddWithValue("@Price", product.PricePerUnit.ToString());
                cmd.Parameters.AddWithValue("@SellBy", product.SellBy.ToString());
                cmd.Parameters.AddWithValue("@BestBefore", product.BestBefore.ToString());
                cmd.Parameters.AddWithValue("@Storage", product.StorageLocation ?? string.Empty);
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
        public Product GetProductByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand("SELECT Name, Price, SellBy, BestBefore, Storage FROM Product WHERE Name = @Name", conn);
                cmd.Parameters.AddWithValue("@Name", name);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Product
                    {
                        ProductName = reader["Name"] as string ?? string.Empty,
                        PricePerUnit = ParsePrice(reader["Price"] as string ?? string.Empty),
                        SellBy = ParseInt(reader["SellBy"] as string ?? string.Empty),
                        BestBefore = ParseInt(reader["BestBefore"] as string ?? string.Empty),
                        StorageLocation = reader["Storage"] as string ?? string.Empty
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
                using var cmd = new SqlCommand("DELETE FROM Product WHERE Name = @Name", conn);
                cmd.Parameters.AddWithValue("@Name", name);
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
