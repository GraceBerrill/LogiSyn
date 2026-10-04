using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    public class SyncResult
    {
        public int SqlToMongo { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public bool Success => Failed == 0;

        public override string ToString() =>
            $"Pushed SQL → Mongo: {SqlToMongo}\n" +
            $"Skipped: {Skipped}\n" +
            $"Failed: {Failed}\n" +
            (Messages.Count > 0 ? "\n" + string.Join("\n", Messages) : "");
    }

    public class SyncService
    {
        private readonly MongoUserService _mongo;
        private readonly UserService _sql;

        public SyncService(MongoUserService mongo, UserService sql)
        {
            _mongo = mongo;
            _sql = sql;
        }

        public SyncResult SyncUsers()
        {
            var result = new SyncResult();

            List<UserRow> sqlUsers;
            try
            {
                sqlUsers = _sql.GetAllUsers();
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Messages.Add("Could not read SQL: " + ex.Message);
                return result;
            }

            var orphans = sqlUsers.Where(u => string.IsNullOrEmpty(u.Id)).ToList();

            if (orphans.Count == 0)
            {
                result.Messages.Add("Everything is already in sync. Nothing to do.");
                return result;
            }

            foreach (var s in orphans)
            {
                try
                {
                    if (_mongo.UsernameExists(s.Name))
                    {
                        result.Skipped++;
                        result.Messages.Add($"Skipped '{s.Name}' — already exists in Mongo.");
                        continue;
                    }

                    var fullPassword = _sql.GetPasswordHashBySqlId(s.SqlId);
                    if (string.IsNullOrEmpty(fullPassword))
                    {
                        result.Skipped++;
                        result.Messages.Add($"Skipped '{s.Name}' — no password hash in SQL.");
                        continue;
                    }

                    var newId = _mongo.AddUserWithHash(s.Name, fullPassword, s.Role, s.DateAdded);
                    if (!string.IsNullOrEmpty(newId))
                        _sql.SetMongoIdForSqlRow(s.SqlId, newId);

                    result.SqlToMongo++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add($"Failed '{s.Name}': {ex.Message}");
                }
            }

            return result;
        }
    }
}