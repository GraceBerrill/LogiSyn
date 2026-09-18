using System;
using Microsoft.Data.SqlClient;
using LogiSyn.Model;

namespace LogiSyn.Services
{
    public class LoginService
    {
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;";

        public User? Authenticate(string name, string password)
        {
            User? user = null;

            string query =
                "SELECT Id, Name, Password, Role " +
                "FROM [User] " +
                "WHERE Name = @Name AND Password = @Password";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Password", password);

                try
                {
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                Id = (int)reader["Id"],
                                Username = reader["Name"] as string ?? string.Empty,
                                Password = reader["Password"] as string ?? string.Empty,
                                Role = reader["Role"] as string ?? string.Empty
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "Database connection error: " + ex.Message);
                }
            }

            return user;
        }
    }
}