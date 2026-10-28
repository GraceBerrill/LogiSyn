using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SharedLibrary.Interface;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Repositories;

namespace AndersonsBakeryAPI.Services
{
    // Product catalog service coordinating Mongo, SQL Server, and local JSON persistence
    public class ProductService : IProductService
    {
        private readonly MongoProductRepository? _mongoRepository;
        private readonly ProductSqlStorage _sqlStorage;
        private readonly ProductFileStorage _fileStorage;
        private readonly ILogger<ProductService>? _logger;

        public ProductService() : this(CreateDefaultMongoRepository()) { }

        public ProductService(MongoProductRepository? mongoRepository, ILogger<ProductService>? logger = null)
            : this(mongoRepository, new ProductSqlStorage(logger), new ProductFileStorage(logger), logger) { }

        public ProductService(
            MongoProductRepository? mongoRepository,
            ProductSqlStorage sqlStorage,
            ProductFileStorage fileStorage,
            ILogger<ProductService>? logger = null)
        {
            _mongoRepository = mongoRepository;
            _sqlStorage = sqlStorage ?? new ProductSqlStorage(logger);
            _fileStorage = fileStorage ?? new ProductFileStorage(logger);
            _logger = logger;
        }

        // Handles the creation of the default MongoProductRepository instance
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
                return null;
            }
        }

        // Gets products from Mongo, SQL, or local JSON file in order of preference
        public List<Product> GetAllProducts()
        {
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

            var sqlProducts = _sqlStorage.LoadProductsFromSql();
            if (sqlProducts != null && sqlProducts.Count > 0)
            {
                EnsureIngredientsFromCatalog(sqlProducts);
                SeedMongoProductsIfEmpty(sqlProducts);
                return sqlProducts;
            }

            var fileProducts = _fileStorage.LoadProductsFromFile();
            if (fileProducts != null && fileProducts.Count > 0)
            {
                EnsureIngredientsFromCatalog(fileProducts);
                SeedMongoProductsIfEmpty(fileProducts);
                return fileProducts;
            }

            var fallbackCatalog = DefaultProductCatalog();
            _fileStorage.SaveCatalogToFile(fallbackCatalog);
            SeedMongoProductsIfEmpty(fallbackCatalog);
            return fallbackCatalog;
        }

        // Gets product display rows for UI consumption
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
                _logger?.LogWarning(ex, "Error in GetAll when loading products");
            }

            return ProductCatalogDefaults.SampleDefaults();
        }

        // Adds full Product model to Mongo, SQL, and local JSON
        public void Add(Product product)
        {
            if (product == null) return;

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

            _sqlStorage.AddProduct(product);

            if (product.ProductID > 0 && _mongoRepository != null)
            {
                try { _mongoRepository.Update(product); } catch { }
            }

            _fileStorage.PersistProductToFile(product);
        }

        public void Add(ProductRow row)
        {
            if (row == null) return;

            var newProd = new Product
            {
                ProductID = row.ProductId,
                ProductName = row.Name ?? string.Empty,
                PricePerUnit = ProductFileStorage.ParsePrice(row.Price ?? string.Empty),
                SellBy = ProductFileStorage.ParseInt(row.SellBy ?? string.Empty),
                BestBefore = ProductFileStorage.ParseInt(row.BestBefore ?? string.Empty),
                StorageLocation = row.Storage ?? string.Empty,
                Method = string.Empty,
                Ingredients = new List<IngredientRequirement>()
            };

            Add(newProd);
        }

        // Updates existing product by name/id across Mongo, SQL, and local JSON
        public void UpdateFromDetail(Product product)
        {
            if (product == null || string.IsNullOrEmpty(product.ProductName)) return;

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

            _sqlStorage.UpdateProduct(product);
            _fileStorage.UpdateInFile(product);
        }

        // Gets a single product by name or id across Mongo, SQL, and local JSON
        public Product? GetProductByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

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

            var sqlProd = _sqlStorage.GetProductByName(name);
            if (sqlProd != null) return sqlProd;

            return _fileStorage.GetProductByName(name);
        }

        // Deletes a product by name or id across Mongo, SQL, and local JSON
        public void DeleteByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

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

            _sqlStorage.DeleteByName(name);
            _fileStorage.DeleteByName(name);
        }

        // Ensures products have default ingredients and recipes if missing
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

        // Seeds products to Mongo if the collection is empty
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

        // Returns default product catalog via ProductCatalogDefaults helper
        public static List<Product> DefaultProductCatalog() => ProductCatalogDefaults.DefaultProductCatalog();
    }
}
