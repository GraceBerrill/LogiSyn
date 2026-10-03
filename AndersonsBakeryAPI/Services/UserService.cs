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
                "SELECT Id, Username, Password, Role, DateAdded " +
                "FROM [User] ORDER BY Id";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            conn.Open();

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new UserRow
                {
                    Id = ((int)reader["Id"]).ToString("D2"),
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

        public bool UsernameExists(string username, string? excludedId = null)
        {
            const string query =
                "SELECT COUNT(1) FROM [User] " +
                "WHERE Username = @Username " +
                "AND (@ExcludedId IS NULL OR Id <> @ExcludedId)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Username", username);

            var excludedIdParameter = cmd.Parameters.Add("@ExcludedId", System.Data.SqlDbType.Int);
            excludedIdParameter.Value = string.IsNullOrEmpty(excludedId)
                ? DBNull.Value
                : int.Parse(excludedId);

            conn.Open();

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public void AddUser(string username, string password, string role)
        {
            const string query =
                "INSERT INTO [User] (Username, Password, Role) " +
                "VALUES (@Username, @Password, @Role)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", PasswordHasher.HashPassword(password));
            cmd.Parameters.AddWithValue("@Role", role);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdateUser(string id, string username, string role, string? newPassword = null)
        {
            const string queryWithPassword =
                "UPDATE [User] " +
                "SET Username = @Username, Password = @Password, Role = @Role " +
                "WHERE Id = @Id";

            const string queryWithoutPassword =
                "UPDATE [User] " +
                "SET Username = @Username, Role = @Role " +
                "WHERE Id = @Id";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                string.IsNullOrWhiteSpace(newPassword)
                    ? queryWithoutPassword
                    : queryWithPassword,
                conn);

            cmd.Parameters.AddWithValue("@Id", int.Parse(id));
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Role", role);

            if (!string.IsNullOrWhiteSpace(newPassword))
                cmd.Parameters.AddWithValue("@Password", PasswordHasher.HashPassword(newPassword));

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteUser(string id)
        {
            const string query = "DELETE FROM [User] WHERE Id = @Id";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Id", int.Parse(id));

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}