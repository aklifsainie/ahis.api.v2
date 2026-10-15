using FluentResults;

namespace ahis.template.identity.Interfaces;

public interface IIdentityApiClientAdministrationService
{
    Task<Result> ValidateAsync(string actorUserId, string? proof, CancellationToken cancellationToken = default);
}
