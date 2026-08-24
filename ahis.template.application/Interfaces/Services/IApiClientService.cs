using ahis.template.application.Features.ApiKeyAuthenticationFeatures.Commands;
using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Interfaces.Services
{
    public interface IApiClientService
    {
        Task<CreateApiClientResponseVM> CreateClientAsync(CreateApiClientCommand command, string? createdBy, CancellationToken cancellationToken = default);

        Task<CreateApiClientKeyResponseVM> CreateKeyAsync(Guid apiClientId, CreateApiClientKeyCommand command, string? createdBy, CancellationToken cancellationToken = default);

        Task RevokeKeyAsync(Guid apiClientId, Guid keyId, string reason, string? revokedBy, CancellationToken cancellationToken = default);

        Task DeactivateClientAsync(Guid apiClientId, string reason, string? deactivatedBy, CancellationToken cancellationToken = default);
    }
}
