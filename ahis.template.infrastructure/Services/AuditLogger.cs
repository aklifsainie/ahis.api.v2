using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.Entities;
using ahis.template.infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(ApplicationDbContext context, ICurrentUserService currentUserService, ILogger<AuditLogger> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
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

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _context.Entry(log).State = EntityState.Detached;
                throw;
            }
            catch (Exception exception)
            {
                _context.Entry(log).State = EntityState.Detached;
                _logger.LogError(exception, "Failed to write audit log for {EntityName} with ID {EntityId}", entityName, entityId);
            }
        }
    }
}
