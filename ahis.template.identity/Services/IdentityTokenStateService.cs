using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ahis.template.identity.Services;

public sealed class IdentityTokenStateService : IIdentityTokenStateService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityContext _context;

    public IdentityTokenStateService(UserManager<ApplicationUser> userManager, IdentityContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public string? GetSecurityVersion(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.SecurityStamp)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.SecurityStamp)));

    public bool IsEligible(ApplicationUser? user) =>
        user is not null && user.IsActive && !user.IsDeleted && !user.IsLockedOut;

    public bool Matches(ApplicationUser? user, string? presentedVersion)
    {
        if (!IsEligible(user) || string.IsNullOrWhiteSpace(presentedVersion))
            return false;

        var currentVersion = GetSecurityVersion(user!);
        if (currentVersion is null || presentedVersion.Length != currentVersion.Length ||
            !presentedVersion.All(Uri.IsHexDigit))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(presentedVersion), Convert.FromHexString(currentVersion));
    }

    public async Task<bool> ValidateAsync(string? userId, string? presentedVersion)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(presentedVersion))
            return false;

        var user = await _userManager.FindByIdAsync(userId);
        return Matches(user, presentedVersion);
    }

    public async Task<IdentityResult> InvalidateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
            return result;

        var now = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(token => token.UserId == user.Id && !token.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.IsRevoked, true)
                .SetProperty(token => token.RevokedAt, now), cancellationToken);

        return IdentityResult.Success;
    }
}
