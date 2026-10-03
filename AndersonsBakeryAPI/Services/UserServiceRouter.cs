using System;
using System.Collections.Generic;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class UserServiceRouter
    {
        private readonly MongoUserService _mongo = new MongoUserService();
        private readonly UserService _sql = new UserService();

        public List<UserRow> GetAllUsers()
        {
            try { return _mongo.GetAllUsers(); }
            catch { return _sql.GetAllUsers(); }
        }

        public bool UsernameExists(string username, string? excludedMongoId = null)
        {
            try { return _mongo.UsernameExists(username, excludedMongoId); }
            catch { return _sql.UsernameExists(username, excludedMongoId); }
        }

        public void AddUser(string username, string plainPassword, string role)
        {
            string hash = PasswordHasher.HashPassword(plainPassword);
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
            try
            {
                _mongo.UpdateUser(mongoId, username, role, newPlainPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mongo update failed: " + ex.Message);
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
            }
        }

        public void DeleteUser(string mongoId)
        {
            try { _mongo.DeleteUser(mongoId); }
            catch (Exception ex) { Console.WriteLine("Mongo delete failed: " + ex.Message); }

            try { _sql.DeleteUser(mongoId); }
            catch (Exception ex) { Console.WriteLine("SQL delete failed: " + ex.Message); }
        }
    }
}