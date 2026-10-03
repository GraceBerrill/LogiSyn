using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SharedLibrary.Model;

namespace LogiSyn.Services
{
    public class LoginService
    {
        private string GetConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            if (!string.IsNullOrEmpty(env))
                return env;

            // Fallback to LocalDB for machines that have it installed.
            return @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";
        }

        public User? Authenticate(string name, string password)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
                return null;

            // Attempt database authentication first. If DB is unavailable, fall back to local JSON store.
            string query =
                "SELECT Id, Username, Password, Role " +
                "FROM [User] " +
                "WHERE Username = @Username AND Password = @Password";

            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", name);
                    cmd.Parameters.AddWithValue("@Password", password);

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = (int)reader["Id"],
                                Username = reader["Username"] as string ?? string.Empty,
                                Password = reader["Password"] as string ?? string.Empty,
                                Role = reader["Role"] as string ?? string.Empty
                            };
                        }
                    }
                }
            }
            catch (SqlException)
            {
                // Fall through to local store
            }
            catch (Exception)
            {
                // Fall through to local store
            }

            // Local JSON fallback (offline-first): Data/users.json in app folder
            var users = LoadLocalUsers();
            return users.FirstOrDefault(u => string.Equals(u.Username, name, StringComparison.OrdinalIgnoreCase)
                                             && u.Password == password);
        }

        private List<User> LoadLocalUsers()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            string usersFile = Path.Combine(dataFolder, "users.json");
            if (!File.Exists(usersFile))
            {
                var defaultUsers = new List<User>
                {
                    new User { Id = 1, Username = "admin",   Password = "1234", Role = "Admin" },
                    new User { Id = 2, Username = "user",    Password = "1234", Role = "User" },
                    new User { Id = 3, Username = "manager", Password = "1234", Role = "Manager" }
                };
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(usersFile, JsonSerializer.Serialize(defaultUsers, options));
                return defaultUsers;
            }

            try
            {
                var json = File.ReadAllText(usersFile);
                var list = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();

                // Ensure test accounts exist (admin, user, manager).
                bool changed = false;
                if (!list.Any(u => string.Equals(u.Username, "admin", StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new User { Id = (list.Count > 0 ? list.Max(x => x.Id) + 1 : 1), Username = "admin", Password = "1234", Role = "Admin" });
                    changed = true;
                }
                if (!list.Any(u => string.Equals(u.Username, "user", StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new User { Id = (list.Count > 0 ? list.Max(x => x.Id) + 1 : 2), Username = "user", Password = "1234", Role = "User" });
                    changed = true;
                }
                if (!list.Any(u => string.Equals(u.Username, "manager", StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new User { Id = (list.Count > 0 ? list.Max(x => x.Id) + 1 : 3), Username = "manager", Password = "1234", Role = "Manager" });
                    changed = true;
                }

                if (changed)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(usersFile, JsonSerializer.Serialize(list, options));
                }

                return list;
            }
            catch
            {
                // If file corrupt, recreate with default set
                var defaultUsers = new List<User>
                {
                    new User { Id = 1, Username = "admin",   Password = "1234", Role = "Admin" },
                    new User { Id = 2, Username = "user",    Password = "1234", Role = "User" },
                    new User { Id = 3, Username = "manager", Password = "1234", Role = "Manager" }
                };
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(usersFile, JsonSerializer.Serialize(defaultUsers, options));
                return defaultUsers;
            }
        }
    }
}
