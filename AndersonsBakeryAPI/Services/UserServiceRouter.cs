using System;
using System.Collections.Generic;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class UserServiceRouter
    {
        private readonly MongoUserService? _mongo;
        private readonly UserService _sql;

        public UserServiceRouter()
        {
            var conn = MongoConfiguration.TryGetConnectionString();
            if (!string.IsNullOrWhiteSpace(conn))
            {
                try
                {
                    _mongo = new MongoUserService(conn, MongoConfiguration.GetDatabaseName());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONGO] Could not initialize MongoUserService: {ex.Message}");
                    _mongo = null;
                }
            }
            _sql = new UserService();
        }

        public UserServiceRouter(MongoUserService mongo, UserService sql)
        {
            _mongo = mongo ?? throw new ArgumentNullException(nameof(mongo));
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
        }

        public List<UserRow> GetAllUsers()
        {
            if (_mongo == null)
                return _sql.GetAllUsers();

            try { return _mongo.GetAllUsers(); }
            catch { return _sql.GetAllUsers(); }
        }

        public bool UsernameExists(string username, string? excludedMongoId = null)
        {
            if (_mongo == null)
                return _sql.UsernameExists(username, excludedMongoId);

            try { return _mongo.UsernameExists(username, excludedMongoId); }
            catch { return _sql.UsernameExists(username, excludedMongoId); }
        }

        public void AddUser(string username, string plainPassword, string role)
        {
            string hash = PasswordHasher.HashPassword(plainPassword);

            if (_mongo == null)
            {
                _sql.AddUser(username, hash, role, string.Empty);
                return;
            }

            string mongoId = string.Empty;
            bool mongoOk = false;
            bool sqlOk = false;

            try
            {
                mongoId = _mongo.AddUserWithHash(username, hash, role);
                mongoOk = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mongo write failed: " + ex.Message);
            }

            try
            {
                _sql.AddUser(username, hash, role, mongoId);
                sqlOk = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL write failed: " + ex.Message);
            }

            if (!mongoOk && !sqlOk)
                throw new Exception("Both databases failed for AddUser.");
        }

        public void UpdateUser(string mongoId, string username, string role, string? newPlainPassword = null)
        {
            if (_mongo != null && !string.IsNullOrWhiteSpace(mongoId) && mongoId.Length == 24 && !int.TryParse(mongoId, out _))
            {
                try
                {
                    _mongo.UpdateUser(mongoId, username, role, newPlainPassword);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Mongo update failed: " + ex.Message);
                }
            }

            string? hash = string.IsNullOrWhiteSpace(newPlainPassword)
                ? null
                : PasswordHasher.HashPassword(newPlainPassword);

            try
            {
                _sql.UpdateUser(mongoId, username, role, hash);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL update failed: " + ex.Message);
                if (_mongo == null || string.IsNullOrWhiteSpace(mongoId) || int.TryParse(mongoId, out _))
                    throw;
            }
        }

        public void DeleteUser(string mongoId)
        {
            if (_mongo != null && !string.IsNullOrWhiteSpace(mongoId) && mongoId.Length == 24 && !int.TryParse(mongoId, out _))
            {
                try { _mongo.DeleteUser(mongoId); }
                catch (Exception ex) { Console.WriteLine("Mongo delete failed: " + ex.Message); }
            }

            try { _sql.DeleteUser(mongoId); }
            catch (Exception ex)
            {
                Console.WriteLine("SQL delete failed: " + ex.Message);
                if (_mongo == null || string.IsNullOrWhiteSpace(mongoId) || int.TryParse(mongoId, out _))
                    throw;
            }
        }
    }
}