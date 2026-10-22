using System.Security.Cryptography;

namespace GiveAid.Helpers
{
    /// <summary>
    /// Secure password hashing using PBKDF2 (SHA-256, 100 000 iterations, random salt).
    /// Stored format:  iterations.saltBase64.hashBase64
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;     // 128-bit salt
        private const int KeySize = 32;      // 256-bit hash
        private const int Iterations = 100_000;

        /// <summary>Creates a hash for a new password.</summary>
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        /// <summary>Checks a login password against the stored hash.</summary>
        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;
            var parts = storedHash.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations) || iterations < 1) return false;
            try
            {
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expected = Convert.FromBase64String(parts[2]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

                // constant-time comparison prevents timing attacks
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;   // damaged hash in the database: treat as wrong password instead of crashing
            }
        }
    }
}
