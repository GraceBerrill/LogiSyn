using System;
using System.Linq;
using MongoDB.Driver;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class MongoLoginService
    {
        private readonly IMongoCollection<UserRow> _usersCollection;

        public MongoLoginService()
            : this(MongoConfig.ConnectionString, MongoConfig.DatabaseName)
        {
        }

        public MongoLoginService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _usersCollection = database.GetCollection<UserRow>("Users");
        }

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