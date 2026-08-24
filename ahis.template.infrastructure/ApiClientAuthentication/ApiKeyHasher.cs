using System.Text;
using System.Security.Cryptography;

namespace ahis.template.infrastructure.ApiClientAuthentication
{
    public static class ApiKeyHasher
    {
        public static string Hash(string rawApiKey)
        {
            if (string.IsNullOrWhiteSpace(rawApiKey))
            {
                throw new ArgumentException("API key cannot be empty.", nameof(rawApiKey));
            }

            var keyBytes = Encoding.UTF8.GetBytes(rawApiKey);
            var hashBytes = SHA256.HashData(keyBytes);

            return Convert.ToHexString(hashBytes).ToUpperInvariant();
        }
    }
}
