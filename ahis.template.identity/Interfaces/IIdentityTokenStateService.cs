using ahis.template.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace ahis.template.identity.Interfaces;

public interface IIdentityTokenStateService
{
    const string SecurityVersionClaim = "security_version";

    string? GetSecurityVersion(ApplicationUser user);
    bool IsEligible(ApplicationUser? user);
    bool Matches(ApplicationUser? user, string? presentedVersion);
    Task<bool> ValidateAsync(string? userId, string? presentedVersion);
    Task<IdentityResult> InvalidateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
