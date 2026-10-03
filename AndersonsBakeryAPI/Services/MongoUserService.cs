using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class MongoUserService
    {
        private readonly IMongoCollection<UserRow> _usersCollection;

        public MongoUserService()
            : this(MongoConfig.ConnectionString, MongoConfig.DatabaseName)
        {
        }

        public MongoUserService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _usersCollection = database.GetCollection<UserRow>("Users");
        }

        public List<UserRow> GetAllUsers()
        {
            var users = _usersCollection.Find(_ => true).ToList();
            foreach (var user in users) user.Password = string.Empty;
            return users;
        }

        public List<UserRow> GetAllUsersRaw()
        {
            return _usersCollection.Find(_ => true).ToList();
        }

        public UserRow? GetById(string id)
        {
            return _usersCollection.Find(u => u.Id == id).FirstOrDefault();
        }

        public bool UsernameExists(string username, string? excludedId = null)
        {
            var filter = Builders<UserRow>.Filter.Eq(u => u.Name, username);
            if (!string.IsNullOrEmpty(excludedId))
                filter &= Builders<UserRow>.Filter.Ne(u => u.Id, excludedId);
            return _usersCollection.Find(filter).Any();
        }

        public string AddUserWithHash(string username, string passwordHash, string role,
                                       string? dateAdded = null)
        {
            var newUser = new UserRow
            {
                Name = username,
                Password = passwordHash,
                Role = role,
                DateAdded = dateAdded ?? DateTime.Now.ToString("yyyy-MM-dd")
            };
            _usersCollection.InsertOne(newUser);
            return newUser.Id;
        }

        public string AddUser(string username, string plainPassword, string role)
        {
            return AddUserWithHash(username, PasswordHasher.HashPassword(plainPassword), role);
        }

        public void UpdateUser(string id, string username, string role, string? newPlainPassword = null)
        {
            var filter = Builders<UserRow>.Filter.Eq(u => u.Id, id);
            var updates = new List<UpdateDefinition<UserRow>>
            {
                Builders<UserRow>.Update.Set(u => u.Name, username),
                Builders<UserRow>.Update.Set(u => u.Role, role)
            };

            if (!string.IsNullOrWhiteSpace(newPlainPassword))
                updates.Add(Builders<UserRow>.Update.Set(u => u.Password,
                    PasswordHasher.HashPassword(newPlainPassword)));

            _usersCollection.UpdateOne(filter, Builders<UserRow>.Update.Combine(updates));
        }

        public void DeleteUser(string id)
        {
            _usersCollection.DeleteOne(Builders<UserRow>.Filter.Eq(u => u.Id, id));
        }
    }
}