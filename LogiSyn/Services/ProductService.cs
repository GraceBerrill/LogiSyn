using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using LogiSyn.Model;

namespace LogiSyn.Services
{
    public class ProductService
    {
        //connection string
        private string GetConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            if (!string.IsNullOrEmpty(env))
                return env;
            return @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";
        }

        /********************************************************************************************/
        //path to the products.json file
        private string ProductsFilePath()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            return Path.Combine(dataFolder, "products.json");
        }

        /********************************************************************************************/
        //get all products from SQL or json
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
            catch
            {
            }
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
                    var prodList = JsonSerializer.Deserialize<List<LogiSyn.Model.Product>>(json);
                    if (prodList != null)
                    {
                        var rows = prodList.ConvertAll(p => new ProductRow
                        {
                            Name = p.ProductName ?? string.Empty,
                            Price = p.PricePerUnit > 0 ? ("R" + p.PricePerUnit.ToString("0.00")) : string.Empty,
                            SellBy = p.SellBy.ToString(),
                            BestBefore = p.BestBefore.ToString(),
                            Storage = p.StorageLocation ?? string.Empty
                        });
                        return rows;
                    }
                }
                catch
                {

                }

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

        /********************************************************************************************/
        //create a new product
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
            catch
            {

            }
            var file = ProductsFilePath();
            List<Product> prodList;
            if (File.Exists(file))
            {
                try
                {
                    prodList = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file)) ?? new List<Product>();
                }
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
                    catch
                    {
                        prodList = new List<Product>();
                    }
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
                Ingredients = new System.Collections.Generic.List<IngredientRequirement>()
            };

            prodList.Add(newProd);
            File.WriteAllText(file, JsonSerializer.Serialize(prodList, new JsonSerializerOptions { WriteIndented = true }));
        }

        /********************************************************************************************/
        //update an existing product by matching name
        public void UpdateFromDetail(LogiSyn.Model.Product product)
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
            catch
            {
            }

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

        /********************************************************************************************/
        //delete a product by name
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
            catch
            {

            }

            var file = ProductsFilePath();
            if (!File.Exists(file)) return;
            try
            {
                var list = JsonSerializer.Deserialize<List<ProductRow>>(File.ReadAllText(file)) ?? new List<ProductRow>();
                var filtered = list.Where(p => !string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
                File.WriteAllText(file, JsonSerializer.Serialize(filtered, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        //sample data
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
/*********************************************MAR26EOF*******************************************/