using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using LogiSyn.Model;

namespace LogiSyn.Services
{
    public class UserService
    {
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";

        public List<User> GetAllUsers()
        {
            var users = new List<User>();
            const string query =
                "SELECT Id, Username, Password, Role, DateAdded FROM [User] ORDER BY Id";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = (int)reader["Id"],
                    Username = reader["Username"] as string ?? string.Empty,
                    Password = reader["Password"] as string ?? string.Empty,
                    Role = reader["Role"] as string ?? string.Empty,
                    DateAdded = reader["DateAdded"] == DBNull.Value
                                ? DateTime.MinValue
                                : (DateTime)reader["DateAdded"]
                });
            }
            return users;
        }

        public bool UsernameExists(string username)
        {
            const string query = "SELECT COUNT(1) FROM [User] WHERE Username = @Username";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            conn.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }

        public void AddUser(string username, string password, string role)
        {
            const string query =
                "INSERT INTO [User] (Username, Password, Role) VALUES (@Username, @Password, @Role)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);
            cmd.Parameters.AddWithValue("@Role", role);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteUser(int id)
        {
            const string query = "DELETE FROM [User] WHERE Id = @Id";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}