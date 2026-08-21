using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM
{
    public class ApiKeyValidationResult
    {
        public bool IsValid { get; init; }

        public Guid? ApiClientDatabaseId { get; init; }

        public Guid? ApiClientKeyId { get; init; }

        public string? ClientId { get; init; }

        public string? ClientName { get; init; }

        public int RateLimitPerMinute { get; init; }

        public IReadOnlyCollection<string> Permissions { get; init; }
            = Array.Empty<string>();

        public string? ErrorCode { get; init; }

        public static ApiKeyValidationResult Success(
            Guid apiClientDatabaseId,
            Guid apiClientKeyId,
            string clientId,
            string clientName,
            int rateLimitPerMinute,
            IReadOnlyCollection<string> permissions)
        {
            return new ApiKeyValidationResult
            {
                IsValid = true,
                ApiClientDatabaseId = apiClientDatabaseId,
                ApiClientKeyId = apiClientKeyId,
                ClientId = clientId,
                ClientName = clientName,
                RateLimitPerMinute = rateLimitPerMinute,
                Permissions = permissions
            };
        }

        public static ApiKeyValidationResult Failure(
            string errorCode)
        {
            return new ApiKeyValidationResult
            {
                IsValid = false,
                ErrorCode = errorCode
            };
        }
    }
}
