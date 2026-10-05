// Adriaan
using SharedLibrary.Interface;
using SharedLibrary.Model;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndersonsBakeryAPI.Repositories
{
    public class MongoOrderRepository : IOrderRepository
    {
        private static bool _indexesInitialized;
        private static readonly object _indexLock = new();

        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<OrderScaled> _ordersCollection;
        private readonly Microsoft.Extensions.Logging.ILogger<MongoOrderRepository>? _logger;

        //------------------------------------------------------------------------------------------------//

        // Registers class mapping for OrderScaled and ProductionItem to handle serialization/deserialization with MongoDB
        static MongoOrderRepository()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(OrderScaled)))
            {
                BsonClassMap.RegisterClassMap<OrderScaled>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(c => c.OrderId);

                    // productionItems is a [JsonIgnore] alias — AutoMap handles PascalCase ProductionItems correctly;
                    // UnmapProperty removed to allow production items to persist to MongoDB.
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // Register the ProductionItem class mapping if not already registered
            if (!BsonClassMap.IsClassMapRegistered(typeof(ProductionItem)))
            {
                BsonClassMap.RegisterClassMap<ProductionItem>(cm =>
                {
                    cm.AutoMap();

                    // packaging is a [JsonIgnore] alias — AutoMap handles PascalCase Packaging correctly;
                    // UnmapProperty removed to allow packaging data to persist to MongoDB.
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Constructor for the MongoOrderRepository class
        public MongoOrderRepository(IMongoDatabase database, Microsoft.Extensions.Logging.ILogger<MongoOrderRepository>? logger = null)
        {
            _logger = logger;
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _ordersCollection = _database.GetCollection<OrderScaled>("Orders");

            // Initialize indexes for better query performance
            InitializeIndexes();
        }

        //------------------------------------------------------------------------------------------------//

        // Creates indexes on the Orders collection for efficient querying (run once)
        private void InitializeIndexes()
        {
            if (_indexesInitialized) return;
            Task.Run(() =>
            {
                lock (_indexLock)
                {
                    if (_indexesInitialized) return;
                    try
                    {
                        // Create an index on the OrderId field for faster lookups
                        var indexKeysDefinition = Builders<OrderScaled>.IndexKeys.Ascending(o => o.OrderId);
                        var indexModel = new CreateIndexModel<OrderScaled>(indexKeysDefinition);
                        _ordersCollection.Indexes.CreateOne(indexModel);

                        var statusIndexKeys = Builders<OrderScaled>.IndexKeys.Ascending(o => o.Status);
                        var statusIndexModel = new CreateIndexModel<OrderScaled>(statusIndexKeys);
                        _ordersCollection.Indexes.CreateOne(statusIndexModel);

                        _indexesInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to create MongoDB indexes");
                    }
                }
            });
        }

        //------------------------------------------------------------------------------------------------//

        // Retrieves all orders from the MongoDB collection
        public async Task<IEnumerable<OrderScaled>> GetAllOrdersAsync()
        {
            try
            {
                var orders = await _ordersCollection.Find(_ => true).ToListAsync();
                return orders;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error retrieving orders from MongoDB");
                return new List<OrderScaled>();
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Retrieves an order by its ID
        public async Task<OrderScaled?> GetOrderByIdAsync(string orderId)
        {
            try
            {
                var filter = Builders<OrderScaled>.Filter.Eq(o => o.OrderId, orderId);
                return await _ordersCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error retrieving order {OrderId} from MongoDB", orderId);
                return null;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Saves or updates an order in the MongoDB collection
        public async Task<OrderScaled> SaveOrderAsync(OrderScaled order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            try
            {
                _logger?.LogDebug("Attempting to save order {OrderId} to MongoDB", order.OrderId);

                var filter = Builders<OrderScaled>.Filter.Eq(o => o.OrderId, order.OrderId);
                var options = new ReplaceOptions { IsUpsert = true };

                var result = await _ordersCollection.ReplaceOneAsync(filter, order, options);

                _logger?.LogDebug("ReplaceOneAsync result for {OrderId}: ModifiedCount={ModifiedCount}, UpsertedId={UpsertedId}", order.OrderId, result.ModifiedCount, result.UpsertedId);
                _logger?.LogInformation("Order {OrderId} saved successfully to MongoDB", order.OrderId);

                return order;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR saving order {OrderId} to MongoDB", order.OrderId);
                throw;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Deletes an order by its ID
        public async Task<bool> DeleteOrderAsync(string orderId)
        {
            try
            {
                var filter = Builders<OrderScaled>.Filter.Eq(o => o.OrderId, orderId);
                var result = await _ordersCollection.DeleteOneAsync(filter);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error deleting order {OrderId} from MongoDB", orderId);
                return false;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Updates the status of an order by its ID
        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
        {
            try
            {
                var filter = Builders<OrderScaled>.Filter.Eq(o => o.OrderId, orderId);
                var update = Builders<OrderScaled>.Update.Set(o => o.Status, status);

                var result = await _ordersCollection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error updating status for order {OrderId} in MongoDB", orderId);
                return false;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Retrieves all orders with the status "Completed"
        public async Task<IEnumerable<OrderScaled>> GetCompletedOrdersAsync()
        {
            try
            {
                var filter = Builders<OrderScaled>.Filter.Eq(o => o.Status, "Completed");
                var orders = await _ordersCollection.Find(filter).ToListAsync();
                return orders;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error retrieving completed orders from MongoDB");
                return new List<OrderScaled>();
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//