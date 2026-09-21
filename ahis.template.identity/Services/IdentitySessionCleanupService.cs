using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ahis.template.identity.Services;

public sealed class IdentitySessionCleanupService : IIdentitySessionCleanupService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan TokenRetention = TimeSpan.FromDays(7);
    private static readonly TimeSpan SessionRetention = TimeSpan.FromDays(30);
    private readonly IdentityContext _context;

    public IdentitySessionCleanupService(IdentityContext context)
    {
        _context = context;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var tokenCutoff = now.Subtract(TokenRetention);
        var tokenIds = await _context.RefreshTokens
            .Where(token => token.ExpiresAt < tokenCutoff &&
                            !_context.RefreshTokens.Any(child => child.ParentTokenId == token.Id))
            .OrderBy(token => token.Id)
            .Select(token => token.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (tokenIds.Count > 0)
        {
            await _context.RefreshTokens
                .Where(token => tokenIds.Contains(token.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var sessionCutoff = now.Subtract(SessionRetention);
        var sessionIds = await _context.RefreshSessions
            .Where(session =>
                              ((session.IsRevoked && session.RevokedAt < sessionCutoff) ||
                               (!session.IsRevoked && session.ExpiresAt < sessionCutoff)) &&
                              !_context.RefreshTokens.Any(token => token.SessionId == session.Id))
            .OrderBy(session => session.Id)
            .Select(session => session.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (sessionIds.Count > 0)
        {
            await _context.RefreshSessions
                .Where(session => sessionIds.Contains(session.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
