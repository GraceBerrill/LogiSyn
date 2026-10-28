using System;
using System.Security.Cryptography;

namespace AndersonsBakeryAPI.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 600_000;
        private const string Algorithm = "PBKDF2-SHA256";

        public static string HashPassword(string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool IsHash(string storedValue)
        {
            return !string.IsNullOrWhiteSpace(storedValue)
                && storedValue.StartsWith(Algorithm + "$", StringComparison.Ordinal);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (password == null || string.IsNullOrWhiteSpace(storedHash))
                return false;

            string[] parts = storedHash.Split('$');

            if (parts.Length != 4 || !parts[0].Equals(Algorithm, StringComparison.Ordinal))
                return false;

            if (!int.TryParse(parts[1], out int iterations) || iterations <= 0)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
