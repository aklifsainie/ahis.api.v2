using ahis.template.identity.Models.Entities;
using FluentResults;

namespace ahis.template.identity.Interfaces;

public interface IAccountSecurityProofService
{
    Task<Result<string>> CreateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<bool> IsValidAsync(string userId, string? proof, CancellationToken cancellationToken = default);
}
