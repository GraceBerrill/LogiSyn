using System;
using Microsoft.Data.SqlClient;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class LoginService
    {
        private string GetConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            return string.IsNullOrWhiteSpace(env)
                ? @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;"
                : env;
        }

        public UserRow? Authenticate(string name, string password)
        {
            const string query =
                "SELECT Id, MongoId, Username, Password, Role, DateAdded " +
                "FROM [Users] " +
                "WHERE Username = @Username";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", name);

            try
            {
                conn.Open();

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                string storedPassword = reader["Password"] as string ?? string.Empty;
                bool isHashed = PasswordHasher.IsHash(storedPassword);

                bool valid;
                if (isHashed)
                {
                    valid = PasswordHasher.VerifyPassword(password, storedPassword);
                }
                else
                {
                    // Constant-time comparison for legacy plaintext migration to prevent timing leakage
                    byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(password);
                    byte[] storedBytes = System.Text.Encoding.UTF8.GetBytes(storedPassword);
                    valid = inputBytes.Length == storedBytes.Length &&
                            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(inputBytes, storedBytes);
                }

                if (!valid)
                    return null;

                var user = new UserRow
                {
                    SqlId = ((int)reader["Id"]).ToString(),
                    Id = reader["MongoId"] as string ?? string.Empty,
                    Name = reader["Username"] as string ?? string.Empty,
                    Password = string.Empty, // Zero-out in memory for security
                    Role = reader["Role"] as string ?? string.Empty,
                    DateAdded = reader["DateAdded"] == DBNull.Value
                        ? string.Empty
                        : ((DateTime)reader["DateAdded"]).ToString("yyyy-MM-dd")
                };

                reader.Close();

                if (!isHashed)
                {
                    string upgradedHash = PasswordHasher.HashPassword(password);

                    using var updateCmd = new SqlCommand(
                    "UPDATE [Users] SET Password = @Password WHERE Id = @Id",
                        conn);

                    updateCmd.Parameters.AddWithValue("@Password", upgradedHash);
                    updateCmd.Parameters.AddWithValue("@Id", int.Parse(user.SqlId));
                    updateCmd.ExecuteNonQuery();
                }

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Database connection error: " + ex.Message, ex);
            }
        }
    }
}