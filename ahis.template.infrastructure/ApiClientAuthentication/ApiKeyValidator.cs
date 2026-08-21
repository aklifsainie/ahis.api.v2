using ahis.template.application.Interfaces.Validators;
using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using ahis.template.infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.ApiClientAuthentication
{
    public class ApiKeyValidator : IApiKeyValidator
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ApiKeyValidator> _logger;

        public ApiKeyValidator(ApplicationDbContext dbContext, ILogger<ApiKeyValidator> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ApiKeyValidationResult> ValidateAsync(string rawApiKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawApiKey))
            {
                return ApiKeyValidationResult.Failure("api_key_missing");
            }

            var keyHash = ApiKeyHasher.Hash(rawApiKey);
            var apiClientKey = await _dbContext.ApiClientKey.AsNoTracking().Include(x => x.ApiClient).ThenInclude(x => x.Permissions).FirstOrDefaultAsync(x => x.KeyHash == keyHash, cancellationToken);

            if (apiClientKey is null)
            {
                _logger.LogWarning("An unknown API key was supplied.");
                return ApiKeyValidationResult.Failure("api_key_invalid");
            }

            if (!apiClientKey.IsActive || apiClientKey.RevokedAt.HasValue)
            {
                _logger.LogWarning("Revoked or inactive API key {KeyPrefix} was used.", apiClientKey.KeyPrefix);
                return ApiKeyValidationResult.Failure("api_key_revoked");
            }

            if (apiClientKey.ExpiresAt.HasValue && apiClientKey.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _logger.LogWarning("Expired API key {KeyPrefix} was used.", apiClientKey.KeyPrefix);
                return ApiKeyValidationResult.Failure("api_key_expired");
            }

            if (!apiClientKey.ApiClient.IsActive)
            {
                _logger.LogWarning("Inactive API client {ClientId} attempted access.", apiClientKey.ApiClient.ClientId);
                return ApiKeyValidationResult.Failure("api_client_inactive");
            }

            var permissions = apiClientKey.ApiClient.Permissions.Select(x => x.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return ApiKeyValidationResult.Success(apiClientKey.ApiClient.Id, apiClientKey.Id, apiClientKey.ApiClient.ClientId, apiClientKey.ApiClient.ClientName, apiClientKey.ApiClient.RateLimitPerMinute, permissions);
        }

        public async Task UpdateLastUsedAsync(Guid apiClientKeyId, CancellationToken cancellationToken = default)
        {
            /*
             * This performs an UPDATE without loading the entity.
             * Available in EF Core 7 and later.
             */
            await _dbContext.ApiClientKey.Where(x => x.Id == apiClientKeyId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastUsedAt, DateTime.UtcNow), cancellationToken);
        }
    }
}
