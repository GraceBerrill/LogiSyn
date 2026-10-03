using System;
using SharedLibrary.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace LogiSyn.Services
{
    public class LoginService
    {
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";

        public UserRow? Authenticate(string name, string password)
        {
            const string query =
                "SELECT Id, Username, Password, Role, DateAdded " +
                "FROM [User] " +
                "WHERE Username = @Username";

            using var conn = new SqlConnection(_connectionString);
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

                bool valid = isHashed
                    ? PasswordHasher.VerifyPassword(password, storedPassword)
                    : string.Equals(password, storedPassword, StringComparison.Ordinal);

                if (!valid)
                    return null;

                var user = new UserRow
                {
                    Id = ((int)reader["Id"]).ToString("D2"),
                    Name = reader["Username"] as string ?? string.Empty,
                    Password = storedPassword,
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
                        "UPDATE [User] SET Password = @Password WHERE Id = @Id",
                        conn);

                    updateCmd.Parameters.AddWithValue("@Password", upgradedHash);
                    updateCmd.Parameters.AddWithValue("@Id", int.Parse(user.Id));
                    updateCmd.ExecuteNonQuery();

                    user.Password = upgradedHash;
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
