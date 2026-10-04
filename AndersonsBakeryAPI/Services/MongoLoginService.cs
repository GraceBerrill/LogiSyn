using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class MongoLoginService
    {
        private readonly IMongoCollection<UserRow> _usersCollection;

        //------------------------------------------------------------------------------------------------//

        public MongoLoginService(IMongoClient client, IConfiguration configuration)
        {
            var databaseName = configuration["MongoDatabase"]
                ?? throw new InvalidOperationException("Missing 'MongoDatabase' value.");

            var database = client.GetDatabase(databaseName);
            _usersCollection = database.GetCollection<UserRow>("Users");
        }

        //------------------------------------------------------------------------------------------------//

        public MongoLoginService(string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("MongoDB connection string is required.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("MongoDB database name is required.", nameof(databaseName));
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            settings.ConnectTimeout = TimeSpan.FromSeconds(3);
            var client = new MongoClient(settings);
            _usersCollection = client.GetDatabase(databaseName).GetCollection<UserRow>("Users");
        }

        //------------------------------------------------------------------------------------------------//

        public UserRow? Authenticate(string name, string password)
        {
            var user = _usersCollection.Find(u => u.Name == name).FirstOrDefault();
            if (user == null) return null;

            string storedPassword = user.Password ?? string.Empty;
            bool isHashed = PasswordHasher.IsHash(storedPassword);
            bool valid = isHashed
                ? PasswordHasher.VerifyPassword(password, storedPassword)
                : string.Equals(password, storedPassword, StringComparison.Ordinal);

            if (!valid) return null;

            if (!isHashed)
            {
                string upgradedHash = PasswordHasher.HashPassword(password);
                _usersCollection.UpdateOne(
                    Builders<UserRow>.Filter.Eq(u => u.Id, user.Id),
                    Builders<UserRow>.Update.Set(u => u.Password, upgradedHash));
                user.Password = upgradedHash;
            }
            return user;
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//