using ahis.template.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace ahis.template.identity.Interfaces;

public interface IIdentityTokenStateService
{
    const string SecurityVersionClaim = "security_version";
    const string TokenUseClaim = "token_use";
    const string AccessTokenUse = "access";

    string? GetSecurityVersion(ApplicationUser user);
    bool IsEligible(ApplicationUser? user);
    bool Matches(ApplicationUser? user, string? presentedVersion);
    Task<bool> MatchesAsync(ApplicationUser? user, string? presentedVersion, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(string? userId, string? presentedVersion, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAsync(string? userId, Guid? sessionPublicId, CancellationToken cancellationToken = default);
    Task<IdentityResult> InvalidateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
