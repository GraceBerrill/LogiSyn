using System;
using System.Diagnostics;
using AndersonsBakeryAPI.Data;
using AndersonsBakeryAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace AndersonsBakeryAPI.Services
{
    // Factory responsible for initializing default SQL Server and MongoDB order repositories
    public static class OrderRepositoryFactory
    {
        // Creates a default SQL repository for order storage, connecting to the local SQL Server database
        public static SqlOrderRepository? CreateDefaultSqlRepository()
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<LogiSynDbContext>();
                optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;TrustServerCertificate=True;");
                var context = new LogiSynDbContext(optionsBuilder.Options);
                context.Database.EnsureCreated();
                return new SqlOrderRepository(context);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[SQL] Could not initialize SQL repository: {ex.Message}");
                return null;
            }
        }

        // Creates a default MongoDB repository for order storage, connecting to MongoDB Atlas
        public static MongoOrderRepository? CreateDefaultMongoRepository()
        {
            try
            {
                string? conn = MongoConfiguration.TryGetConnectionString();
                if (string.IsNullOrWhiteSpace(conn)) return null;

                string dbName = MongoConfiguration.GetDatabaseName();
                var settings = MongoClientSettings.FromConnectionString(conn);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(8);
                settings.ConnectTimeout = TimeSpan.FromSeconds(8);
                var client = new MongoClient(settings);
                var db = client.GetDatabase(dbName);
                return new MongoOrderRepository(db);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[MONGODB] Could not initialize MongoDB repository: {ex.Message}");
                return null;
            }
        }
    }
}
/*********************************************MAR26EOF*******************************************/