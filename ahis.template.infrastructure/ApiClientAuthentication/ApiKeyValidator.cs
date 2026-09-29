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
        private readonly TimeProvider _timeProvider;

        public ApiKeyValidator(ApplicationDbContext dbContext, ILogger<ApiKeyValidator> logger, TimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _logger = logger;
            _timeProvider = timeProvider;
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

            if (apiClientKey.ExpiresAt.HasValue && apiClientKey.ExpiresAt.Value <= _timeProvider.GetUtcNow().UtcDateTime)
            {
                _logger.LogWarning("Expired API key {KeyPrefix} was used.", apiClientKey.KeyPrefix);
                return ApiKeyValidationResult.Failure("api_key_expired");
            }

            if (!apiClientKey.ApiClient.IsActive)
            {
                _logger.LogWarning("Inactive API client {ClientId} attempted access.", apiClientKey.ApiClient.ClientId);
                return ApiKeyValidationResult.Failure("api_client_inactive");
            }

            if (apiClientKey.ApiClient.RateLimitPerMinute is < 1 or > 10000)
            {
                _logger.LogError("API client {ClientId} has an invalid configured rate limit.", apiClientKey.ApiClient.Id);
                return ApiKeyValidationResult.Failure("api_client_invalid_configuration");
            }

            var permissions = apiClientKey.ApiClient.Permissions.Select(x => x.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return ApiKeyValidationResult.Success(apiClientKey.ApiClient.Id, apiClientKey.Id, apiClientKey.ApiClient.ClientId, apiClientKey.ApiClient.ClientName, apiClientKey.ApiClient.RateLimitPerMinute, permissions, apiClientKey.LastUsedAt);
        }

        public async Task UpdateLastUsedAsync(Guid apiClientKeyId, DateTime? observedLastUsedAt, CancellationToken cancellationToken = default)
        {
            /*
             * This performs an UPDATE without loading the entity.
             * Available in EF Core 7 and later.
             */
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (observedLastUsedAt.HasValue && observedLastUsedAt.Value > now.AddMinutes(-1))
                return;

            await _dbContext.ApiClientKey
                .Where(x => x.Id == apiClientKeyId &&
                    (!x.LastUsedAt.HasValue || x.LastUsedAt <= now.AddMinutes(-1)))
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastUsedAt, now), cancellationToken);
        }
    }
}
