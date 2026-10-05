using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace AndersonsBakeryAPI.Services
{
    public static class MongoConfiguration
    {

        public static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public static string? TryGetConnectionString()
        {
            try
            {
                var configuration = BuildConfiguration();
                var conn = configuration.GetConnectionString("MongoConnection")
                    ?? configuration["MongoConnection"];
                return string.IsNullOrWhiteSpace(conn) ? null : conn;
            }
            catch
            {
                return null;
            }
        }

        public static string GetConnectionString()
        {
            var conn = TryGetConnectionString();

            if (string.IsNullOrWhiteSpace(conn))
            {
                throw new InvalidOperationException(
                    "MongoDB connection string was not found. Configure ConnectionStrings:MongoConnection in User Secrets.");
            }

            return conn;
        }

        public static string GetDatabaseName()
        {
            try
            {
                var configuration = BuildConfiguration();
                return configuration["MongoDatabase"] ?? "LogiSynDb";
            }
            catch
            {
                return "LogiSynDb";
            }
        }
    }
}
