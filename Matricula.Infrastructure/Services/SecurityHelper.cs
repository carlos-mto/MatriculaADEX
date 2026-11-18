namespace Matricula.Infrastructure.Services
{
    public static class SecurityHelper
    {
        private const int Iterations = 100_000;
        private const int HashBytes = 32;

        public static string GenerateSalt() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        public static string HashPassword(string password, string salt) =>
            Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
                password, Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA256, HashBytes));

        public static bool VerifyPassword(string password, string salt, string hash)
        {
            try
            {
                var expected = Convert.FromBase64String(hash);
                var actual = Rfc2898DeriveBytes.Pbkdf2(
                    password, Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA256, HashBytes);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
