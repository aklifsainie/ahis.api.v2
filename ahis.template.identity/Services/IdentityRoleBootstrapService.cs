using System.Data;
using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ahis.template.identity.Services;

public sealed class IdentityRoleBootstrapService : IIdentityRoleBootstrapService
{
    private static readonly string[] SystemRoleNames =
    [
        IdentityRoleNames.Superadmin,
        IdentityRoleNames.User
    ];

    private readonly IdentityContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILookupNormalizer _normalizer;
    private readonly IIdentityTokenStateService _tokenState;
    private readonly IIdentityRestrictionService _restrictions;
    private readonly IOptions<IdentityRoleBootstrapOptions> _options;
    private readonly ILogger<IdentityRoleBootstrapService> _logger;

    public IdentityRoleBootstrapService(
        IdentityContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILookupNormalizer normalizer,
        IIdentityTokenStateService tokenState,
        IIdentityRestrictionService restrictions,
        IOptions<IdentityRoleBootstrapOptions> options,
        ILogger<IdentityRoleBootstrapService> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _normalizer = normalizer;
        _tokenState = tokenState;
        _restrictions = restrictions;
        _options = options;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSystemRolesAsync(cancellationToken);

        var configuredEmail = _options.Value.SuperadminEmail?.Trim();
        if (string.IsNullOrWhiteSpace(configuredEmail))
            return;

        if (await _context.IdentityRoleBootstrapStates
                .AsNoTracking()
                .AnyAsync(state => state.Id == IdentityRoleBootstrapState.SingletonId, cancellationToken))
        {
            _logger.LogWarning("Identity role bootstrap configuration is present after bootstrap completion.");
            return;
        }

        await BootstrapFirstSuperadminAsync(configuredEmail, cancellationToken);
    }

    private async Task EnsureSystemRolesAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            foreach (var roleName in SystemRoleNames)
            {
                var normalizedName = _normalizer.NormalizeName(roleName);
                var existing = await _context.Roles.SingleOrDefaultAsync(
                    role => role.NormalizedName == normalizedName,
                    cancellationToken);
                if (existing is not null)
                    continue;

                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                    throw new InvalidOperationException("Unable to create required Identity role.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task BootstrapFirstSuperadminAsync(string configuredEmail, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var completed = await _context.IdentityRoleBootstrapStates
                .SingleOrDefaultAsync(state => state.Id == IdentityRoleBootstrapState.SingletonId, cancellationToken);
            if (completed is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogWarning("Identity role bootstrap configuration is present after bootstrap completion.");
                return;
            }

            var superadminRole = await _context.Roles.SingleAsync(
                role => role.NormalizedName == _normalizer.NormalizeName(IdentityRoleNames.Superadmin),
                cancellationToken);
            if (await _context.UserRoles.AnyAsync(
                    assignment => assignment.RoleId == superadminRole.Id,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    "A Superadmin assignment exists without a bootstrap completion record.");
            }

            var normalizedEmail = _normalizer.NormalizeEmail(configuredEmail);
            var targets = await _context.Users
                .Where(user => user.NormalizedEmail == normalizedEmail)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (targets.Count != 1 || !IsEligibleBootstrapTarget(targets[0]) ||
                !await _restrictions.IsAuthenticationAllowedAsync(targets[0], cancellationToken))
            {
                throw new InvalidOperationException("Configured bootstrap account is not eligible.");
            }

            var target = targets[0];
            var assignmentResult = await _userManager.AddToRoleAsync(target, IdentityRoleNames.Superadmin);
            if (!assignmentResult.Succeeded)
                throw new InvalidOperationException("Unable to assign the first Superadmin role.");

            var invalidationResult = await _tokenState.InvalidateAsync(target, cancellationToken);
            if (!invalidationResult.Succeeded)
                throw new InvalidOperationException("Unable to invalidate the bootstrap account token state.");

            _context.IdentityRoleBootstrapStates.Add(new IdentityRoleBootstrapState
            {
                TargetUserId = target.Id,
                CompletedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Identity role bootstrap completed.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static bool IsEligibleBootstrapTarget(ApplicationUser user) =>
        user.EmailConfirmed && user.IsActive && !user.IsDeleted && !user.IsLockedOut;
}
