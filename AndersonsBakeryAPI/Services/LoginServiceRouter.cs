using System;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class LoginServiceRouter
    {
        private readonly MongoLoginService _mongo = new MongoLoginService();
        private readonly LoginService _sql = new LoginService();

        public UserRow? Authenticate(string name, string password)
        {
            try
            {
                var user = _mongo.Authenticate(name, password);
                if (user != null)
                    return user;   // found online → done
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                Console.WriteLine("Mongo unreachable: " + ex.Message);
            }

            return _sql.Authenticate(name, password);
        }

        private static bool IsNetworkError(Exception ex)
        {
            var name = ex.GetType().Name;
            if (name.Contains("MongoConnection") ||
                name.Contains("Timeout") ||
                name.Contains("Socket"))
                return true;

            return ex.InnerException != null && IsNetworkError(ex.InnerException);
        }
    }
}