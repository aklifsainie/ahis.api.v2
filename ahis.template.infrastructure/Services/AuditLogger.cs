using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.Entities;
using ahis.template.infrastructure.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AuditLogger(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task LogAsync(
        AuditActionEnum action,
        string entityName,
        string entityId,
        string? metadata = null,
        string? overrideUserId = null,
        string? overrideUserName = null,
        string? overrideUserRole = null,
        CancellationToken cancellationToken = default)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = overrideUserId ?? _currentUserService.UserId,
                UserName = overrideUserName ?? _currentUserService.UserName,
                UserRole = overrideUserRole ?? _currentUserService.UserRole,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                TimestampUtc = DateTime.UtcNow,
                IpAddress = _currentUserService.IpAddress,      // available pre-auth, comes from HttpContext directly
                UserAgent = _currentUserService.UserAgent,       // same
                CorrelationId = _currentUserService.CorrelationId,
                Metadata = metadata
            };

            _context.AuditLog.Add(log);

            // Commit immediately and independently - a read query never calls
            // SaveChanges for business data, so this is the only write happening.
            // If this throws, we deliberately swallow it below rather than fail
            // the citizen's actual read request just because audit logging hiccuped.
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Audit logging must never break the primary user-facing operation.
                // Replace this with your actual logging framework (Serilog, etc.)
                // so failures are still visible to ops - do not leave this empty in prod.
                // _logger.LogError(ex, "Failed to write audit log for {Entity}/{Id}", entityName, entityId);
            }
        }
    }
}
