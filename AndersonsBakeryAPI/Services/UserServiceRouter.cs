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
            catch (Exception ex) when (IsNetworkError(ex)) { return _sql.GetAllUsers(); }
        }

        public bool UsernameExists(string username, string? excludedId = null)
        {
            try { return _mongo.UsernameExists(username, excludedId); }
            catch (Exception ex) when (IsNetworkError(ex)) { return _sql.UsernameExists(username, int.TryParse(excludedId, out var id) ? id : null); }
        }

        public void AddUser(string username, string password, string role)
        {
            try { _mongo.AddUser(username, password, role); }
            catch (Exception ex) when (IsNetworkError(ex)) { _sql.AddUser(username, password, role); }
        }

        public void UpdateUser(string id, string username, string role, string? newPassword = null)
        {
            try { _mongo.UpdateUser(id, username, role, newPassword); }
            catch (Exception ex) when (IsNetworkError(ex)) { _sql.UpdateUser(int.Parse(id), username, role, newPassword); }
        }

        public void DeleteUser(string id)
        {
            try { _mongo.DeleteUser(id); }
            catch (Exception ex) when (IsNetworkError(ex)) { _sql.DeleteUser(int.Parse(id)); }
        }

        private static bool IsNetworkError(Exception ex) { /* same as above */ }
    }
}