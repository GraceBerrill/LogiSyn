using System;
using Microsoft.Data.SqlClient;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class LoginService
    {
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";

        public UserRow? Authenticate(string name, string password)
        {
<<<<<<< HEAD
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            return string.IsNullOrWhiteSpace(env)
                ? @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;"
                : env;
        }

        public UserRow? Authenticate(string name, string password)
        {
            const string query =
                "SELECT Id, MongoId, Username, Password, Role, DateAdded " +
                "FROM [User] " +
                "WHERE Username = @Username";

            using var conn = new SqlConnection(GetConnectionString());
=======
            const string query =
                "SELECT Id, Username, Password, Role, DateAdded " +
                "FROM [User] " +
                "WHERE Username = @Username";

            using var conn = new SqlConnection(_connectionString);
>>>>>>> Adriaan
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", name);

            try
            {
                conn.Open();
<<<<<<< HEAD
=======

>>>>>>> Adriaan
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                string storedPassword = reader["Password"] as string ?? string.Empty;
<<<<<<< HEAD
                bool isHashed = PasswordHasher.IsHash(storedPassword);
=======

                bool isHashed = PasswordHasher.IsHash(storedPassword);

>>>>>>> Adriaan
                bool valid = isHashed
                    ? PasswordHasher.VerifyPassword(password, storedPassword)
                    : string.Equals(password, storedPassword, StringComparison.Ordinal);

                if (!valid)
                    return null;

                var user = new UserRow
                {
<<<<<<< HEAD
                    SqlId = ((int)reader["Id"]).ToString(),
                    Id = reader["MongoId"] as string ?? string.Empty,
=======
                    Id = ((int)reader["Id"]).ToString("D2"),
>>>>>>> Adriaan
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
<<<<<<< HEAD
                    updateCmd.Parameters.AddWithValue("@Id", int.Parse(user.SqlId));
=======
                    updateCmd.Parameters.AddWithValue("@Id", int.Parse(user.Id));
>>>>>>> Adriaan
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