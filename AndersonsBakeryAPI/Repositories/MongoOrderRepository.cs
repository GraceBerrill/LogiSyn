using LogiSyn.Interface;
using LogiSyn.Model;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndersonsBakeryAPI.Repositories
{
    /// <summary>
    /// MongoDB implementation of IOrderRepository using MongoDB.Driver
    /// </summary>
    public class MongoOrderRepository : IOrderRepository
    {
        private static bool _indexesInitialized;
        private static readonly object _indexLock = new();

        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<OrderScaled> _ordersCollection;

        static MongoOrderRepository()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(OrderScaled)))
            {
                BsonClassMap.RegisterClassMap<OrderScaled>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(c => c.OrderId);
                    cm.UnmapProperty(c => c.productionItems);
                    cm.SetIgnoreExtraElements(true);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(ProductionItem)))
            {
                BsonClassMap.RegisterClassMap<ProductionItem>(cm =>
                {
                    cm.AutoMap();
                    cm.UnmapProperty(c => c.packaging);
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Constructor for the MongoOrderRepository class
        public MongoOrderRepository(IMongoDatabase database)
        {
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
                        var indexKeysDefinition = Builders<OrderScaled>.IndexKeys.Ascending(o => o.OrderId);
                        var indexModel = new CreateIndexModel<OrderScaled>(indexKeysDefinition);
                        _ordersCollection.Indexes.CreateOne(indexModel);

                        // Also create an index on Status for filtering completed orders
                        var statusIndexKeys = Builders<OrderScaled>.IndexKeys.Ascending(o => o.Status);
                        var statusIndexModel = new CreateIndexModel<OrderScaled>(statusIndexKeys);
                        _ordersCollection.Indexes.CreateOne(statusIndexModel);

                        _indexesInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to create MongoDB indexes: {ex.Message}");
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
                Console.WriteLine($"Error retrieving orders from MongoDB: {ex.Message}");
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
                Console.WriteLine($"Error retrieving order {orderId} from MongoDB: {ex.Message}");
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
                Console.WriteLine($"[MONGO_REPO] Attempting to save order {order.OrderId}");

                var filter = Builders<OrderScaled>.Filter.Eq(o => o.OrderId, order.OrderId);
                var options = new ReplaceOptions { IsUpsert = true };

                var result = await _ordersCollection.ReplaceOneAsync(filter, order, options);

                Console.WriteLine($"[MONGO_REPO] ReplaceOneAsync result: ModifiedCount={result.ModifiedCount}, UpsertedId={result.UpsertedId}");
                Console.WriteLine($"[MONGO_REPO] ✓ Order {order.OrderId} saved successfully");

                return order;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO_REPO] ✗ ERROR saving order {order.OrderId}: {ex.Message}");
                Console.WriteLine($"[MONGO_REPO] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[MONGO_REPO] Stack Trace: {ex.StackTrace}");
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
                Console.WriteLine($"Error deleting order {orderId} from MongoDB: {ex.Message}");
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
                Console.WriteLine($"Error updating status for order {orderId} in MongoDB: {ex.Message}");
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
                Console.WriteLine($"Error retrieving completed orders from MongoDB: {ex.Message}");
                return new List<OrderScaled>();
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
