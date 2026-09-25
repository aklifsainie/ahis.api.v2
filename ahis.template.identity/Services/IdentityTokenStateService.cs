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
    private readonly IIdentityRestrictionService _restrictions;

    public IdentityTokenStateService(UserManager<ApplicationUser> userManager, IdentityContext context, IIdentityRestrictionService restrictions)
    {
        _userManager = userManager;
        _context = context;
        _restrictions = restrictions;
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

    public async Task<bool> MatchesAsync(ApplicationUser? user, string? presentedVersion, CancellationToken cancellationToken = default)
    {
        if (!Matches(user, presentedVersion))
            return false;
        return await _restrictions.IsAuthenticationAllowedAsync(user, cancellationToken);
    }

    public async Task<bool> ValidateAsync(string? userId, string? presentedVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(presentedVersion))
            return false;

        var user = await _userManager.FindByIdAsync(userId);
        return await MatchesAsync(user, presentedVersion, cancellationToken);
    }

    public async Task<bool> ValidateSessionAsync(
        string? userId,
        Guid? sessionPublicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || !sessionPublicId.HasValue)
            return false;

        var now = DateTime.UtcNow;
        return await _context.RefreshSessions
            .AsNoTracking()
            .AnyAsync(session =>
                session.UserId == userId &&
                session.PublicId == sessionPublicId.Value &&
                !session.IsRevoked &&
                session.ExpiresAt > now,
                cancellationToken);
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

        await _context.RefreshSessions
            .Where(session => session.UserId == user.Id && !session.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.IsRevoked, true)
                .SetProperty(session => session.RevokedAt, now), cancellationToken);

        return IdentityResult.Success;
    }
}
