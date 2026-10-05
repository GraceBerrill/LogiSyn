using System;
using System.Collections.Generic;
using SharedLibrary.Model;
using Microsoft.Extensions.Logging;

namespace AndersonsBakeryAPI.Services
{
    public class UserServiceRouter
    {
        private readonly MongoUserService? _mongo;
        private readonly UserService _sql;
        private readonly ILogger<UserServiceRouter>? _logger;
        public UserServiceRouter(ILogger<UserServiceRouter>? logger = null)
        {
            _logger = logger;
            var conn = MongoConfiguration.TryGetConnectionString();
            if (!string.IsNullOrWhiteSpace(conn))
            {
                try
                {
                    _mongo = new MongoUserService(conn, MongoConfiguration.GetDatabaseName());
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not initialize MongoUserService; continuing in SQL-only mode.");
                    _mongo = null;
                }
            }
            _sql = new UserService();
        }

        public UserServiceRouter(MongoUserService mongo, UserService sql, ILogger<UserServiceRouter>? logger = null)
        {
            _mongo = mongo ?? throw new ArgumentNullException(nameof(mongo));
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
            _logger = logger;
        }

        public List<UserRow> GetAllUsers()
        {
            if (_mongo == null)
                return _sql.GetAllUsers();

            try { return _mongo.GetAllUsers(); }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Mongo GetAllUsers failed; falling back to SQL.");
                return _sql.GetAllUsers();
            }
        }

        public bool UsernameExists(string username, string? excludedMongoId = null)
        {
            if (_mongo == null)
                return _sql.UsernameExists(username, excludedMongoId);

            try { return _mongo.UsernameExists(username, excludedMongoId); }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Mongo UsernameExists failed; falling back to SQL for username {Username}.", username);
                return _sql.UsernameExists(username, excludedMongoId);
            }
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
                _logger?.LogWarning(ex, "Mongo write failed when adding user {Username}.", username);
            }

            try
            {
                _sql.AddUser(username, hash, role, mongoId);
                sqlOk = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SQL write failed when adding user {Username}.", username);
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
                    _logger?.LogWarning(ex, "Mongo update failed for user {MongoId}.", mongoId);
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
                _logger?.LogError(ex, "SQL update failed for user {MongoId}.", mongoId);
                if (_mongo == null || string.IsNullOrWhiteSpace(mongoId) || int.TryParse(mongoId, out _))
                    throw;
            }
        }

        public void DeleteUser(string mongoId)
        {
            if (_mongo != null && !string.IsNullOrWhiteSpace(mongoId) && mongoId.Length == 24 && !int.TryParse(mongoId, out _))
            {
                try { _mongo.DeleteUser(mongoId); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Mongo delete failed for {MongoId}.", mongoId); }
            }

            try { _sql.DeleteUser(mongoId); }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SQL delete failed for {MongoId}.", mongoId);
                if (_mongo == null || string.IsNullOrWhiteSpace(mongoId) || int.TryParse(mongoId, out _))
                    throw;
            }
        }
    }
}