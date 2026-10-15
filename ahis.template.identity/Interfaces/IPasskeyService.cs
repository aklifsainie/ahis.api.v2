using ahis.template.identity.Models.DTOs;
using FluentResults;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;

namespace ahis.template.identity.Interfaces;

public interface IPasskeyService
{
    Task<Result<AuthenticationResponseVM>> IssuePasskeySessionAsync(string userId, CancellationToken cancellationToken);
    Task<Result<PasskeyOptionsDto>> RegistrationOptionsAsync(string userId, string? ip, CancellationToken cancellationToken);
    Task<Result<PasskeyCredentialDto>> CompleteRegistrationAsync(string userId, PasskeyRegistrationDto request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<PasskeyCredentialDto>>> ListAsync(string userId, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(string userId, Guid credentialId, string? stepUpProof, string? password, string? recoveryCode, CancellationToken cancellationToken);
    Task<Result<PasskeyOptionsDto>> AuthenticationOptionsAsync(string? ip, CancellationToken cancellationToken);
    Task<Result<AuthenticationResponseVM>> CompleteAuthenticationAsync(PasskeyAssertionDto request, string? ip, CancellationToken cancellationToken);
}
