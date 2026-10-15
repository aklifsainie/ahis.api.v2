using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ahis.template.identity.Services;

public sealed class FirstSuperadminBootstrapService : IFirstSuperadminBootstrapService
{
    private readonly IdentityContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IIdentityRestrictionService _restrictions;
    private readonly IIdentityTokenStateService _tokens;

    public FirstSuperadminBootstrapService(IdentityContext context, UserManager<ApplicationUser> users,
        IIdentityRestrictionService restrictions, IIdentityTokenStateService tokens)
        => (_context, _users, _restrictions, _tokens) = (context, users, restrictions, tokens);

    public async Task<bool> BootstrapAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        // The singleton predicate is held under serializable isolation; the PK/check remain the final guard.
        if (await _context.FirstSuperadminBootstraps.AnyAsync(cancellationToken) ||
            await _context.UserRoles.Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.NormalizedName)
                .AnyAsync(name => name == "SUPERADMIN", cancellationToken)) return false;

        var role = await _context.Roles.SingleOrDefaultAsync(x => x.NormalizedName == "SUPERADMIN", cancellationToken);
        var user = await _users.FindByIdAsync(userId);
        if (role is null || user is null || !user.IsActive || user.IsDeleted || !user.EmailConfirmed ||
            user.LockoutEnd.HasValue || await _context.UserRoles.AnyAsync(x => x.UserId == userId, cancellationToken) ||
            !await _restrictions.IsAuthenticationAllowedAsync(user, cancellationToken)) return false;

        // Recheck immediately before mutation.
        if (await _context.FirstSuperadminBootstraps.AnyAsync(cancellationToken) ||
            await _context.UserRoles.Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.NormalizedName)
                .AnyAsync(name => name == "SUPERADMIN", cancellationToken) || user.LockoutEnd.HasValue ||
            !await _restrictions.IsAuthenticationAllowedAsync(user, cancellationToken)) return false;

        var added = await _users.AddToRoleAsync(user, role.Name!);
        if (!added.Succeeded || await _context.UserRoles.CountAsync(x => x.UserId == userId, cancellationToken) != 1) return false;
        var invalidated = await _tokens.InvalidateAsync(user, cancellationToken);
        if (!invalidated.Succeeded) return false;
        _context.FirstSuperadminBootstraps.Add(new FirstSuperadminBootstrap { TargetUserId = userId, CompletedAtUtc = DateTime.UtcNow });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
