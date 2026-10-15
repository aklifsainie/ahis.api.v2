using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ahis.template.identity.Services;

/// <summary>Single Identity-bound reader/writer for restriction provenance.</summary>
public sealed class IdentityRestrictionService : IIdentityRestrictionService
{
    private readonly IdentityContext _context;

    public IdentityRestrictionService(IdentityContext context) => _context = context;

    public async Task<IdentityUserRestriction?> GetEffectiveAsync(string userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var active = await GetActiveAsync(userId, cancellationToken);
        if (active is null)
            return null;

        if (active.Category == IdentityRestrictionCategory.OrdinaryLockout &&
            active.ExpiresAtUtc <= nowUtc)
            return null;

        return active;
    }

    public Task<IdentityUserRestriction?> GetActiveAsync(string userId, CancellationToken cancellationToken = default) =>
        _context.IdentityUserRestrictions
            .SingleOrDefaultAsync(x => x.UserId == userId && x.EndedAtUtc == null, cancellationToken);

    public async Task<bool> CloseOrdinaryLockoutAsync(string userId, string endedByUserId, DateTime endedAtUtc, CancellationToken cancellationToken = default)
    {
        var changed = await _context.IdentityUserRestrictions
            .Where(x => x.UserId == userId && x.EndedAtUtc == null &&
                x.Category == IdentityRestrictionCategory.OrdinaryLockout &&
                x.ExpiresAtUtc > endedAtUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.EndedAtUtc, endedAtUtc)
                .SetProperty(x => x.EndedByUserId, endedByUserId), cancellationToken);
        return changed == 1;
    }

    public async Task<bool> IsAuthenticationAllowedAsync(ApplicationUser? user, CancellationToken cancellationToken = default)
    {
        if (user is null || !user.IsActive || user.IsDeleted)
            return false;

        try
        {
            var now = DateTime.UtcNow;
            var active = await _context.IdentityUserRestrictions
                .Where(x => x.UserId == user.Id && x.EndedAtUtc == null)
                .SingleOrDefaultAsync(cancellationToken);
            if (active is not null)
                return active.Category == IdentityRestrictionCategory.OrdinaryLockout &&
                    active.ExpiresAtUtc <= now;

            // A legacy Identity lockout without provenance is ambiguous and must not be
            // silently treated as ordinary. This is also the guard against bypass writers.
            return !user.LockoutEnd.HasValue;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested == false)
        {
            // A missing/inconsistent provenance read is a denial, never an admission.
            return false;
        }
    }

    public async Task<bool> EnsureOrdinaryLockoutAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        if (!user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow)
            return true;

        var now = DateTime.UtcNow;
        var active = await _context.IdentityUserRestrictions
            .SingleOrDefaultAsync(x => x.UserId == user.Id && x.EndedAtUtc == null, cancellationToken);

        if (active is not null)
        {
            if (active.Category != IdentityRestrictionCategory.OrdinaryLockout)
                return false;

            if (active.ExpiresAtUtc > now)
            {
                active.ExpiresAtUtc = user.LockoutEnd.Value.UtcDateTime;
                return true;
            }

            active.EndedAtUtc = now;
        }

        _context.IdentityUserRestrictions.Add(new IdentityUserRestriction
        {
            UserId = user.Id,
            Category = IdentityRestrictionCategory.OrdinaryLockout,
            StartedAtUtc = now,
            ExpiresAtUtc = user.LockoutEnd.Value.UtcDateTime,
            Origin = "IdentityPasswordFailure"
        });
        return true;
    }
}
