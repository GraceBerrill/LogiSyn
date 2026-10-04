using System;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    /// <summary>
    /// Tries Mongo first. If the network is down, falls back to SQL.
    /// </summary>
    public class LoginServiceRouter
    {
        private readonly MongoLoginService? _mongo;
        private readonly LoginService _sql;

        public LoginServiceRouter()
        {
            _mongo = new MongoLoginService(
                MongoConfiguration.GetConnectionString(),
                MongoConfiguration.GetDatabaseName());
            _sql = new LoginService();
        }

        public LoginServiceRouter(MongoLoginService mongo, LoginService sql)
        {
            _mongo = mongo ?? throw new ArgumentNullException(nameof(mongo));
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
        }

        public UserRow? Authenticate(string name, string password)
        {
            if (_mongo == null)
                return _sql.Authenticate(name, password);

            try
            {
                return _mongo.Authenticate(name, password);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mongo auth failed, falling back to SQL: " + ex.Message);
                return _sql.Authenticate(name, password);
            }
        }
    }
}