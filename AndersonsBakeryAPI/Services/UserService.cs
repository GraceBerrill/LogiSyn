using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
<<<<<<< HEAD
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
=======
using LogiSyn.Model;

namespace LogiSyn.Services
{
    public class UserService
    {
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";
>>>>>>> Adriaan

        public List<UserRow> GetAllUsers()
        {
            var users = new List<UserRow>();

            const string query =
<<<<<<< HEAD
                "SELECT Id, MongoId, Username, Password, Role, DateAdded " +
                "FROM [User] ORDER BY Id";

            using var conn = new SqlConnection(GetConnectionString());
=======
                "SELECT Id, Username, Password, Role, DateAdded " +
                "FROM [User] ORDER BY Id";

            using var conn = new SqlConnection(_connectionString);
>>>>>>> Adriaan
            using var cmd = new SqlCommand(query, conn);
            conn.Open();

            using var reader = cmd.ExecuteReader();
<<<<<<< HEAD
=======

>>>>>>> Adriaan
            while (reader.Read())
            {
                users.Add(new UserRow
                {
<<<<<<< HEAD
                    SqlId = ((int)reader["Id"]).ToString(),
                    Id = reader["MongoId"] as string ?? string.Empty,
=======
                    Id = ((int)reader["Id"]).ToString("D2"),
>>>>>>> Adriaan
                    Name = reader["Username"] as string ?? string.Empty,
                    Password = string.Empty,
                    Role = reader["Role"] as string ?? string.Empty,
                    DateAdded = reader["DateAdded"] == DBNull.Value
                        ? string.Empty
                        : ((DateTime)reader["DateAdded"]).ToString("yyyy-MM-dd")
                });
            }
<<<<<<< HEAD
            return users;
        }

        public bool UsernameExists(string username, string? excludedMongoId = null)
=======

            return users;
        }

        public bool UsernameExists(string username, int? excludedId = null)
>>>>>>> Adriaan
        {
            const string query =
                "SELECT COUNT(1) FROM [User] " +
                "WHERE Username = @Username " +
<<<<<<< HEAD
                "AND (@ExcludedMongoId IS NULL OR MongoId <> @ExcludedMongoId)";

            using var conn = new SqlConnection(GetConnectionString());
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

        public void UpdateUser(string mongoId, string username, string role, string? newPasswordHash = null)
        {
            const string withPassword =
                "UPDATE [User] SET Username = @Username, Password = @Password, Role = @Role " +
                "WHERE MongoId = @MongoId";

            const string withoutPassword =
                "UPDATE [User] SET Username = @Username, Role = @Role " +
                "WHERE MongoId = @MongoId";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(
                string.IsNullOrWhiteSpace(newPasswordHash) ? withoutPassword : withPassword,
                conn);

            cmd.Parameters.AddWithValue("@MongoId", mongoId);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Role", role);
            if (!string.IsNullOrWhiteSpace(newPasswordHash))
                cmd.Parameters.AddWithValue("@Password", newPasswordHash);
=======
                "AND (@ExcludedId IS NULL OR Id <> @ExcludedId)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Username", username);
            var excludedIdParameter = cmd.Parameters.Add("@ExcludedId", System.Data.SqlDbType.Int);
            excludedIdParameter.Value = excludedId.HasValue
                ? excludedId.Value
                : DBNull.Value;

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

        public void UpdateUser(int id, string username, string role, string? newPassword = null)
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

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Role", role);

            if (!string.IsNullOrWhiteSpace(newPassword))
                cmd.Parameters.AddWithValue("@Password", PasswordHasher.HashPassword(newPassword));
>>>>>>> Adriaan

            conn.Open();
            cmd.ExecuteNonQuery();
        }

<<<<<<< HEAD
        public void DeleteUser(string mongoId)
        {
            const string query = "DELETE FROM [User] WHERE MongoId = @MongoId";

            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MongoId", mongoId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public string? GetPasswordHashBySqlId(string sqlId)
        {
            const string query = "SELECT Password FROM [User] WHERE Id = @Id";
            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", int.Parse(sqlId));
            conn.Open();
            return cmd.ExecuteScalar() as string;
        }

        public void SetMongoIdForSqlRow(string sqlId, string mongoId)
        {
            const string query = "UPDATE [User] SET MongoId = @MongoId WHERE Id = @Id";
            using var conn = new SqlConnection(GetConnectionString());
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MongoId", mongoId);
            cmd.Parameters.AddWithValue("@Id", int.Parse(sqlId));
=======
        public void DeleteUser(int id)
        {
            const string query = "DELETE FROM [User] WHERE Id = @Id";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Id", id);

>>>>>>> Adriaan
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> Adriaan
