using System;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    /// <summary>
    /// Tries Mongo first. If the network is down, falls back to SQL.
    /// </summary>
    public class LoginServiceRouter
    {
        private readonly MongoLoginService _mongo = new MongoLoginService();
        private readonly LoginService _sql = new LoginService();

        public UserRow? Authenticate(string name, string password)
        {
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