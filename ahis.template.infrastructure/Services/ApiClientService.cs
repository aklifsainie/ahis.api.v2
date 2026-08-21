using ahis.template.application.Features.ApiKeyAuthenticationFeatures.Commands;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Models.Entities.ApiKey;
using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using ahis.template.infrastructure.ApiClientAuthentication;
using ahis.template.infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Services
{
    public class ApiClientService : IApiClientService
    {
        private readonly ApplicationDbContext _dbContext;

        public ApiClientService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CreateApiClientResponseVM> CreateClientAsync(CreateApiClientCommand command, string? createdBy, CancellationToken cancellationToken = default)
        {
            var normalizedClientId = NormalizeClientId(command.ClientId);

            var clientExists = await _dbContext.ApiClients.AnyAsync(x => x.ClientId == normalizedClientId, cancellationToken);

            if (clientExists)
            {
                throw new InvalidOperationException($"API client '{normalizedClientId}' already exists.");
            }

            if (command.KeyExpiresAt.HasValue && command.KeyExpiresAt.Value <= DateTime.UtcNow)
            {
                throw new ArgumentException("The key expiry date must be in the future.", nameof(command.KeyExpiresAt));
            }

            var generatedKey = ApiKeyGenerator.Generate();
            var utcNow = DateTime.UtcNow;
            var apiClient = new ApiClient
            {
                Id = Guid.NewGuid(),
                ClientId = normalizedClientId,
                ClientName = command.ClientName.Trim(),
                Description = command.Description?.Trim(),
                ContactName = command.ContactName?.Trim(),
                ContactEmail = command.ContactEmail?.Trim(),
                IsActive = true,
                RateLimitPerMinute = command.RateLimitPerMinute,
                CreatedAt = utcNow,
                CreatedBy = createdBy
            };

            var apiClientKey = new ApiClientKey
            {
                Id = Guid.NewGuid(),
                ApiClientId = apiClient.Id,
                KeyName = command.InitialKeyName.Trim(),
                KeyPrefix = generatedKey.KeyPrefix,
                KeyHash = generatedKey.KeyHash,
                IsActive = true,
                CreatedAt = utcNow,
                CreatedBy = createdBy,
                ExpiresAt = command.KeyExpiresAt
            };

            apiClient.Keys.Add(apiClientKey);

            var permissions = command.Permissions
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var permission in permissions)
            {
                apiClient.Permissions.Add(
                    new ApiClientPermission
                    {
                        Id = Guid.NewGuid(),
                        ApiClientId = apiClient.Id,
                        PermissionCode = permission,
                        CreatedAt = utcNow,
                        CreatedBy = createdBy
                    }
                );
            }

            _dbContext.ApiClients.Add(apiClient);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateApiClientResponseVM
            {
                Id = apiClient.Id,
                ClientId = apiClient.ClientId,
                ClientName = apiClient.ClientName,
                KeyName = apiClientKey.KeyName,
                KeyPrefix = apiClientKey.KeyPrefix,

                /*
                 * This is the only place where the raw key
                 * is returned.
                 */
                ApiKey = generatedKey.RawKey,

                KeyExpiresAt = apiClientKey.ExpiresAt,
                Permissions = permissions
            };
        }

        public async Task<CreateApiClientKeyResponseVM> CreateKeyAsync(Guid apiClientId, CreateApiClientKeyCommand command, string? createdBy, CancellationToken cancellationToken = default)
        {
            var apiClient = await _dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == apiClientId, cancellationToken) ?? throw new KeyNotFoundException("API client was not found.");

            if (!apiClient.IsActive)
            {
                throw new InvalidOperationException("Cannot create a key for an inactive API client.");
            }

            if (command.ExpiresAt.HasValue && command.ExpiresAt.Value <= DateTime.UtcNow)
            {
                throw new ArgumentException("The key expiry date must be in the future.", nameof(command.ExpiresAt));
            }

            var generatedKey = ApiKeyGenerator.Generate();
            var apiClientKey = new ApiClientKey
            {
                Id = Guid.NewGuid(),
                ApiClientId = apiClient.Id,
                KeyName = command.KeyName.Trim(),
                KeyPrefix = generatedKey.KeyPrefix,
                KeyHash = generatedKey.KeyHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                ExpiresAt = command.ExpiresAt
            };

            _dbContext.ApiClientKey.Add(apiClientKey);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateApiClientKeyResponseVM
            {
                KeyId = apiClientKey.Id,
                ApiClientId = apiClient.Id,
                KeyName = apiClientKey.KeyName,
                KeyPrefix = apiClientKey.KeyPrefix,
                ApiKey = generatedKey.RawKey,
                ExpiresAt = apiClientKey.ExpiresAt
            };
        }

        public async Task RevokeKeyAsync(Guid apiClientId, Guid keyId, string reason, string? revokedBy, CancellationToken cancellationToken = default)
        {
            var apiClientKey = await _dbContext.ApiClientKey.FirstOrDefaultAsync(x => x.Id == keyId && x.ApiClientId == apiClientId, cancellationToken) ?? throw new KeyNotFoundException("API client key was not found.");

            if (!apiClientKey.IsActive || apiClientKey.RevokedAt.HasValue)
            {
                return;
            }

            apiClientKey.IsActive = false;
            apiClientKey.RevokedAt = DateTime.UtcNow;
            apiClientKey.RevokedBy = revokedBy;
            apiClientKey.RevocationReason = reason.Trim();

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeactivateClientAsync(Guid apiClientId, string reason, string? deactivatedBy, CancellationToken cancellationToken = default)
        {
            var apiClient = await _dbContext.ApiClients.Include(x => x.Keys).FirstOrDefaultAsync(x => x.Id == apiClientId, cancellationToken) ?? throw new KeyNotFoundException("API client was not found.");

            var utcNow = DateTime.UtcNow;

            apiClient.IsActive = false;
            apiClient.DeactivatedAt = utcNow;
            apiClient.DeactivationReason = reason.Trim();
            apiClient.UpdatedAt = utcNow;
            apiClient.UpdatedBy = deactivatedBy;

            /*
             * Deactivate all keys belonging to the client.
             */
            foreach (var key in apiClient.Keys.Where(x => x.IsActive))
            {
                key.IsActive = false;
                key.RevokedAt = utcNow;
                key.RevokedBy = deactivatedBy;
                key.RevocationReason = "API client was deactivated.";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string NormalizeClientId(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("Client ID is required.", nameof(clientId));
            }

            return clientId.Trim().ToLowerInvariant().Replace(' ', '-');
        }
    }
}
