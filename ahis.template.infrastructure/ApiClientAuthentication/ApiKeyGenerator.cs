using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.ApiClientAuthentication
{
    public static class ApiKeyGenerator
    {
        private const string Prefix = "cpj_live_";

        public static GeneratedApiKey Generate()
        {
            /*
             * Generate 32 random bytes = 256 bits of randomness.
             */
            var randomBytes = RandomNumberGenerator.GetBytes(32);

            /*
             * Convert to URL-safe Base64.
             */
            var randomPart = Convert.ToBase64String(randomBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var rawApiKey = $"{Prefix}{randomPart}";

            /*
             * Store only a short, non-secret prefix for identifying
             * the key in the admin portal.
             */
            var keyPrefix = rawApiKey[..Math.Min(rawApiKey.Length, 20)];
            var keyHash = ApiKeyHasher.Hash(rawApiKey);

            return new GeneratedApiKey(rawApiKey, keyPrefix, keyHash);
        }
    }
}
