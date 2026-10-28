using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    // Local JSON file storage handler for bakery products
    public class ProductFileStorage
    {
        private readonly ILogger? _logger;

        public ProductFileStorage(ILogger? logger = null)
        {
            _logger = logger;
        }

        // Returns the path to the products.json file in the Data folder
        public string ProductsFilePath()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            return Path.Combine(dataFolder, "products.json");
        }

        // Reads products from local products.json file, handling both Product and ProductRow schemas
        public List<Product>? LoadProductsFromFile()
        {
            var file = ProductsFilePath();
            if (!File.Exists(file)) return null;

            try
            {
                var prodList = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(file));
                if (prodList != null && prodList.Count > 0)
                {
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
                    var defaults = ProductCatalogDefaults.DefaultProductCatalog();
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
                    return converted;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse legacy product rows from local JSON file.");
            }

            return null;
        }

        // Writes a catalog list to the local JSON file
        public void SaveCatalogToFile(List<Product> catalog)
        {
            var file = ProductsFilePath();
            try
            {
                File.WriteAllText(file, JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write catalog to local JSON.");
            }
        }

        // Persists a product to the local JSON file, updating if existing or appending if new
        public void PersistProductToFile(Product product)
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

        // Updates an existing product in the local JSON file
        public void UpdateInFile(Product product)
        {
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

        // Retrieves a product by name or id from the local JSON file
        public Product? GetProductByName(string name)
        {
            var file = ProductsFilePath();
            if (!File.Exists(file)) return null;

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

            return null;
        }

        // Deletes a product from the local JSON file by name or id
        public void DeleteByName(string name)
        {
            var file = ProductsFilePath();
            if (!File.Exists(file)) return;

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

        public static decimal ParsePrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price)) return 0m;
            var cleaned = price.Replace("R", "").Replace("$", "").Trim();
            if (decimal.TryParse(cleaned, out var v)) return v;
            return 0m;
        }

        public static int ParseInt(string s)
        {
            if (int.TryParse(s, out var v)) return v;
            return 0;
        }
    }
}
