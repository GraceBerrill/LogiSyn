using System;
using SharedLibrary.Model;
using Microsoft.Extensions.Logging;

namespace AndersonsBakeryAPI.Services
{
    /// <summary>
    /// Tries Mongo first. If the network is down, falls back to SQL.
    /// </summary>
    public class LoginServiceRouter
    {
        private readonly MongoLoginService? _mongo;
        private readonly LoginService _sql;
        private readonly ILogger<LoginServiceRouter>? _logger;

        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
        public LoginServiceRouter(ILogger<LoginServiceRouter>? logger = null)
        {
            _logger = logger;
            var conn = MongoConfiguration.TryGetConnectionString();
            if (!string.IsNullOrWhiteSpace(conn))
            {
                try
                {
                    _mongo = new MongoLoginService(conn, MongoConfiguration.GetDatabaseName());
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not initialize MongoLoginService; falling back to SQL authentication.");
                    _mongo = null;
                }
            }
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
                _logger?.LogWarning(ex, "Mongo auth failed; falling back to SQL for user {User}.", name);
                return _sql.Authenticate(name, password);
            }
        }
    }
}