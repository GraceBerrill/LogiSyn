using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class UserService
    {
        private string GetConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("LOGISYN_CONNECTION");
            return string.IsNullOrWhiteSpace(env)
                ? @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;"
                : env;
        }

        public List<UserRow> GetAllUsers()
        {
            var users = new List<UserRow>();

            const string query =
                "SELECT Id, MongoId, Username, Password, Role, DateAdded " +
                "FROM [User] ORDER BY Id";

            try
            {
                using var conn = new SqlConnection(GetConnectionString());
                using var cmd = new SqlCommand(query, conn);
                conn.Open();

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new UserRow
                    {
                        SqlId = ((int)reader["Id"]).ToString(),
                        Id = reader["MongoId"] as string ?? string.Empty,
                        Name = reader["Username"] as string ?? string.Empty,
                        Password = string.Empty,
                        Role = reader["Role"] as string ?? string.Empty,
                        DateAdded = reader["DateAdded"] == DBNull.Value
                            ? string.Empty
                            : ((DateTime)reader["DateAdded"]).ToString("yyyy-MM-dd")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve users from database.", ex);
            }

            return users;
        }

        public bool UsernameExists(string username, string? excludedIdentifier = null)
        {
            int.TryParse(excludedIdentifier, out int excludedId);

            const string query =
                "SELECT COUNT(1) FROM [User] " +
                "WHERE Username = @Username " +
                "AND (@ExcludedIdentifier IS NULL OR @ExcludedIdentifier = '' OR MongoId <> @ExcludedIdentifier) " +
                "AND (@ExcludedId <= 0 OR Id <> @ExcludedId)";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@ExcludedIdentifier",
                string.IsNullOrEmpty(excludedIdentifier) ? DBNull.Value : excludedIdentifier);
            cmd.Parameters.AddWithValue("@ExcludedId", excludedId > 0 ? excludedId : -1);

            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public string AddUser(string username, string passwordHash, string role, string mongoId)
        {
            const string query =
                "INSERT INTO [User] (Username, Password, Role, MongoId) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@Username, @Password, @Role, @MongoId)";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", passwordHash);
            cmd.Parameters.AddWithValue("@Role", role);
            cmd.Parameters.AddWithValue("@MongoId",
                string.IsNullOrEmpty(mongoId) ? DBNull.Value : mongoId);

            conn.Open();
            return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        public void UpdateUser(string identifier, string username, string role, string? newPasswordHash = null)
        {
            const string withPassword =
                "UPDATE [User] SET Username = @Username, Password = @Password, Role = @Role " +
                "WHERE (MongoId IS NOT NULL AND MongoId <> '' AND MongoId = @Identifier) " +
                "   OR (Id = @ParsedId)";

            const string withoutPassword =
                "UPDATE [User] SET Username = @Username, Role = @Role " +
                "WHERE (MongoId IS NOT NULL AND MongoId <> '' AND MongoId = @Identifier) " +
                "   OR (Id = @ParsedId)";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(
                string.IsNullOrWhiteSpace(newPasswordHash) ? withoutPassword : withPassword,
                conn);

            int.TryParse(identifier, out int parsedId);

            cmd.Parameters.AddWithValue("@Identifier", identifier ?? string.Empty);
            cmd.Parameters.AddWithValue("@ParsedId", parsedId > 0 ? parsedId : -1);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Role", role);
            if (!string.IsNullOrWhiteSpace(newPasswordHash))
                cmd.Parameters.AddWithValue("@Password", newPasswordHash);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdateUser(int id, string username, string role, string? newPasswordHash = null)
        {
            const string withPassword =
                "UPDATE [User] SET Username = @Username, Password = @Password, Role = @Role " +
                "WHERE Id = @Id";

            const string withoutPassword =
                "UPDATE [User] SET Username = @Username, Role = @Role " +
                "WHERE Id = @Id";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(
                string.IsNullOrWhiteSpace(newPasswordHash) ? withoutPassword : withPassword,
                conn);

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Role", role);
            if (!string.IsNullOrWhiteSpace(newPasswordHash))
                cmd.Parameters.AddWithValue("@Password", newPasswordHash);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteUser(string identifier)
        {
            const string query =
                "DELETE FROM [User] " +
                "WHERE (MongoId IS NOT NULL AND MongoId <> '' AND MongoId = @Identifier) " +
                "   OR (Id = @ParsedId)";

            int.TryParse(identifier, out int parsedId);

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Identifier", identifier ?? string.Empty);
            cmd.Parameters.AddWithValue("@ParsedId", parsedId > 0 ? parsedId : -1);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteUser(int id)
        {
            const string query = "DELETE FROM [User] WHERE Id = @Id";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public string? GetPasswordHashBySqlId(string sqlId)
        {
            const string query = "SELECT Password FROM [User] WHERE Id = @Id";
            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", int.TryParse(sqlId, out var parsedSqlId) ? parsedSqlId : 0);
            conn.Open();
            return cmd.ExecuteScalar() as string;
        }

        public void SetMongoIdForSqlRow(string sqlId, string mongoId)
        {
            const string query = "UPDATE [User] SET MongoId = @MongoId WHERE Id = @Id";
            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MongoId", mongoId);
            cmd.Parameters.AddWithValue("@Id", int.TryParse(sqlId, out var parsedMongoSqlId) ? parsedMongoSqlId : 0);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}