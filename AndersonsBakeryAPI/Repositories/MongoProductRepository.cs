using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Repositories
{
    /// <summary>
    /// MongoDB implementation for Product collection storage and retrieval.
    /// </summary>
    public class MongoProductRepository
    {
        private static bool _indexesInitialized;
        private static readonly object _indexLock = new();

        private readonly IMongoCollection<Product> _productsCollection;
        private readonly Microsoft.Extensions.Logging.ILogger<MongoProductRepository>? _logger;

        static MongoProductRepository()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Product)))
            {
                BsonClassMap.RegisterClassMap<Product>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(IngredientRequirement)))
            {
                BsonClassMap.RegisterClassMap<IngredientRequirement>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }

        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
        public MongoProductRepository(IMongoDatabase database, Microsoft.Extensions.Logging.ILogger<MongoProductRepository>? logger = null)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            _logger = logger;
            _productsCollection = database.GetCollection<Product>("Products");
            EnsureIndexes();
        }

        public MongoProductRepository(string connectionString, string databaseName, Microsoft.Extensions.Logging.ILogger<MongoProductRepository>? logger = null)
        {
            _logger = logger;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("MongoDB connection string is required.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = "LogiSynDb";

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            settings.ConnectTimeout = TimeSpan.FromSeconds(3);
            var client = new MongoClient(settings);
            _productsCollection = client.GetDatabase(databaseName).GetCollection<Product>("Products");
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            if (_indexesInitialized) return;
            Task.Run(() =>
            {
                lock (_indexLock)
                {
                    if (_indexesInitialized) return;
                    try
                    {
                        var keys = Builders<Product>.IndexKeys.Ascending(p => p.ProductName);
                        var options = new CreateIndexOptions { Unique = true, Sparse = true };
                        _productsCollection.Indexes.CreateOne(new CreateIndexModel<Product>(keys, options));
                        _indexesInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to create Products index");
                    }
                }
            });
        }

        public List<Product> GetAllProducts()
        {
            return _productsCollection.Find(_ => true).ToList();
        }

        public Product? GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var filter = Builders<Product>.Filter.Regex(p => p.ProductName,
                new BsonRegularExpression($"^{Regex.Escape(name.Trim())}$", "i"));
            return _productsCollection.Find(filter).FirstOrDefault();
        }

        public Product? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _productsCollection.Find(p => p.Id == id).FirstOrDefault();
        }

        public Product? GetByProductId(int productId)
        {
            if (productId <= 0) return null;
            return _productsCollection.Find(p => p.ProductID == productId).FirstOrDefault();
        }

        public void Insert(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            _productsCollection.InsertOne(product);
        }

        public bool Update(Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ProductName)) return false;

            FilterDefinition<Product> filter;
            if (!string.IsNullOrWhiteSpace(product.Id))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
            }
            else if (product.ProductID > 0)
            {
                filter = Builders<Product>.Filter.Eq(p => p.ProductID, product.ProductID);
            }
            else
            {
                filter = Builders<Product>.Filter.Regex(p => p.ProductName,
                    new BsonRegularExpression($"^{Regex.Escape(product.ProductName.Trim())}$", "i"));
            }

            var update = Builders<Product>.Update
                .Set(p => p.ProductName, product.ProductName)
                .Set(p => p.PricePerUnit, product.PricePerUnit)
                .Set(p => p.SellBy, product.SellBy)
                .Set(p => p.BestBefore, product.BestBefore)
                .Set(p => p.StorageLocation, product.StorageLocation)
                .Set(p => p.Method, product.Method)
                .Set(p => p.Ingredients, product.Ingredients ?? new List<IngredientRequirement>());

            if (product.ProductID > 0)
            {
                update = update.Set(p => p.ProductID, product.ProductID);
            }

            var result = _productsCollection.UpdateOne(filter, update);
            return result.MatchedCount > 0;
        }

        public void Upsert(Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ProductName)) return;

            FilterDefinition<Product> filter;
            if (!string.IsNullOrWhiteSpace(product.Id))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
            }
            else if (product.ProductID > 0)
            {
                filter = Builders<Product>.Filter.Or(
                    Builders<Product>.Filter.Eq(p => p.ProductID, product.ProductID),
                    Builders<Product>.Filter.Regex(p => p.ProductName,
                        new BsonRegularExpression($"^{Regex.Escape(product.ProductName.Trim())}$", "i"))
                );
            }
            else
            {
                filter = Builders<Product>.Filter.Regex(p => p.ProductName,
                    new BsonRegularExpression($"^{Regex.Escape(product.ProductName.Trim())}$", "i"));
            }

            var existing = _productsCollection.Find(filter).FirstOrDefault();
            if (existing != null)
            {
                product.Id = existing.Id;
                if (product.ProductID <= 0 && existing.ProductID > 0)
                {
                    product.ProductID = existing.ProductID;
                }
                _productsCollection.ReplaceOne(Builders<Product>.Filter.Eq(p => p.Id, existing.Id), product);
            }
            else
            {
                _productsCollection.InsertOne(product);
            }
        }

        public bool DeleteByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var filter = Builders<Product>.Filter.Regex(p => p.ProductName,
                new BsonRegularExpression($"^{Regex.Escape(name.Trim())}$", "i"));
            var result = _productsCollection.DeleteOne(filter);
            return result.DeletedCount > 0;
        }

        public bool DeleteById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            var result = _productsCollection.DeleteOne(p => p.Id == id);
            return result.DeletedCount > 0;
        }

        public bool DeleteByProductId(int productId)
        {
            if (productId <= 0) return false;
            var result = _productsCollection.DeleteOne(p => p.ProductID == productId);
            return result.DeletedCount > 0;
        }

        public void SeedIfEmpty(IEnumerable<Product> catalog)
        {
            try
            {
                if (!_productsCollection.Find(_ => true).Any())
                {
                    var items = catalog.ToList();
                    if (items.Count > 0)
                    {
                        _productsCollection.InsertMany(items);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error seeding products to MongoDB");
            }
        }
    }
}

