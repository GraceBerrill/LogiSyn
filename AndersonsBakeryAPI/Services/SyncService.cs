using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Repositories;

namespace AndersonsBakeryAPI.Services
{
    public class SyncResult
    {
        public int SqlToMongo { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public bool Success => Failed == 0;

        public override string ToString() =>
            $"Pushed SQL → Mongo: {SqlToMongo}\n" +
            $"Skipped: {Skipped}\n" +
            $"Failed: {Failed}\n" +
            (Messages.Count > 0 ? "\n" + string.Join("\n", Messages) : "");
    }

    public class SyncService
    {
        private readonly MongoUserService? _mongo;
        private readonly UserService _sql;
        private readonly MongoProductRepository? _mongoProducts;
        private readonly ProductService _productService;

        public SyncService()
        {
            _sql = new UserService();
            var conn = MongoConfiguration.TryGetConnectionString();
            if (!string.IsNullOrWhiteSpace(conn))
            {
                try
                {
                    _mongo = new MongoUserService(conn, MongoConfiguration.GetDatabaseName());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONGO] Could not initialize MongoUserService for sync: {ex.Message}");
                    _mongo = null;
                }

                try
                {
                    _mongoProducts = new MongoProductRepository(conn, MongoConfiguration.GetDatabaseName());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONGO] Could not initialize MongoProductRepository for sync: {ex.Message}");
                    _mongoProducts = null;
                }
            }
            _productService = new ProductService(_mongoProducts);
        }

        public bool IsMongoConfigured => _mongo != null || _mongoProducts != null;

        public SyncService(MongoUserService? mongo, UserService sql, MongoProductRepository? mongoProducts = null, ProductService? productService = null)
        {
            _mongo = mongo;
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
            _mongoProducts = mongoProducts;
            _productService = productService ?? new ProductService(mongoProducts);
        }

        public SyncResult SyncUsers()
        {
            var result = new SyncResult();

            if (_mongo == null)
            {
                result.Failed = 1;
                result.Messages.Add("MongoDB sync is not configured. SQL users are available locally.");
                return result;
            }

            List<UserRow> sqlUsers;
            try
            {
                sqlUsers = _sql.GetAllUsers();
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Messages.Add("Could not read SQL: " + ex.Message);
                return result;
            }

            var orphans = sqlUsers.Where(u => string.IsNullOrEmpty(u.Id)).ToList();

            if (orphans.Count == 0)
            {
                result.Messages.Add("Everything is already in sync. Nothing to do.");
                return result;
            }

            foreach (var s in orphans)
            {
                try
                {
                    if (_mongo.UsernameExists(s.Name))
                    {
                        result.Skipped++;
                        result.Messages.Add($"Skipped '{s.Name}' — already exists in Mongo.");
                        continue;
                    }

                    var fullPassword = _sql.GetPasswordHashBySqlId(s.SqlId);
                    if (string.IsNullOrEmpty(fullPassword))
                    {
                        result.Skipped++;
                        result.Messages.Add($"Skipped '{s.Name}' — no password hash in SQL.");
                        continue;
                    }

                    var newId = _mongo.AddUserWithHash(s.Name, fullPassword, s.Role, s.DateAdded);
                    if (!string.IsNullOrEmpty(newId))
                        _sql.SetMongoIdForSqlRow(s.SqlId, newId);

                    result.SqlToMongo++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add($"Failed '{s.Name}': {ex.Message}");
                }
            }

            return result;
        }

        public SyncResult SyncProducts()
        {
            var result = new SyncResult();

            if (_mongoProducts == null)
            {
                result.Failed = 1;
                result.Messages.Add("MongoDB is not configured. Products are available locally.");
                return result;
            }

            List<Product> products;
            try
            {
                products = _productService.GetAllProducts();
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Messages.Add("Could not read products: " + ex.Message);
                return result;
            }

            foreach (var p in products)
            {
                try
                {
                    _mongoProducts.Upsert(p);
                    result.SqlToMongo++;
                    result.Messages.Add($"Synchronized product '{p.ProductName}' to Mongo.");
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add($"Failed '{p.ProductName}': {ex.Message}");
                }
            }

            return result;
        }
    }
}