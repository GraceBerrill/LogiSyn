using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class UserService
    {
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";

        public List<UserRow> GetAllUsers()
        {
            var users = new List<UserRow>();

            const string query =
                "SELECT Id, MongoId, Username, Password, Role, DateAdded " +
                "FROM [User] ORDER BY Id";

            using var conn = new SqlConnection(_connectionString);
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
            return users;
        }

        public bool UsernameExists(string username, string? excludedMongoId = null)
        {
            const string query =
                "SELECT COUNT(1) FROM [User] " +
                "WHERE Username = @Username " +
                "AND (@ExcludedMongoId IS NULL OR MongoId <> @ExcludedMongoId)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@ExcludedMongoId",
                string.IsNullOrEmpty(excludedMongoId) ? DBNull.Value : excludedMongoId);

            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public string AddUser(string username, string passwordHash, string role, string mongoId)
        {
            const string query =
                "INSERT INTO [User] (Username, Password, Role, MongoId) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@Username, @Password, @Role, @MongoId)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", passwordHash);
            cmd.Parameters.AddWithValue("@Role", role);
            cmd.Parameters.AddWithValue("@MongoId",
                string.IsNullOrEmpty(mongoId) ? DBNull.Value : mongoId);

            conn.Open();
            return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        public void UpdateUser(string mongoId, string username, string role, string? newPasswordHash = null)
        {
            const string withPassword =
                "UPDATE [User] SET Username = @Username, Password = @Password, Role = @Role " +
                "WHERE MongoId = @MongoId";

            const string withoutPassword =
                "UPDATE [User] SET Username = @Username, Role = @Role " +
                "WHERE MongoId = @MongoId";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                string.IsNullOrWhiteSpace(newPasswordHash) ? withoutPassword : withPassword,
                conn);

            cmd.Parameters.AddWithValue("@MongoId", mongoId);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Role", role);
            if (!string.IsNullOrWhiteSpace(newPasswordHash))
                cmd.Parameters.AddWithValue("@Password", newPasswordHash);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteUser(string mongoId)
        {
            const string query = "DELETE FROM [User] WHERE MongoId = @MongoId";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MongoId", mongoId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public string? GetPasswordHashBySqlId(string sqlId)
        {
            const string query = "SELECT Password FROM [User] WHERE Id = @Id";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", int.Parse(sqlId));
            conn.Open();
            return cmd.ExecuteScalar() as string;
        }

        public void SetMongoIdForSqlRow(string sqlId, string mongoId)
        {
            const string query = "UPDATE [User] SET MongoId = @MongoId WHERE Id = @Id";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MongoId", mongoId);
            cmd.Parameters.AddWithValue("@Id", int.Parse(sqlId));
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}