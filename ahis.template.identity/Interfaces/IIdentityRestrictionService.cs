using ahis.template.identity.Models.Entities;

namespace ahis.template.identity.Interfaces;

public interface IIdentityRestrictionService
{
    Task<bool> IsAuthenticationAllowedAsync(ApplicationUser? user, CancellationToken cancellationToken = default);
    Task<bool> EnsureOrdinaryLockoutAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<IdentityUserRestriction?> GetEffectiveAsync(string userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<IdentityUserRestriction?> GetActiveAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> CloseOrdinaryLockoutAsync(string userId, string endedByUserId, DateTime endedAtUtc, CancellationToken cancellationToken = default);
}
