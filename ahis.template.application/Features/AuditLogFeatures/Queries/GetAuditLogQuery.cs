using ahis.template.application.Interfaces.Repositories;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.Entities;
using ahis.template.domain.Models.ViewModels.AuditLogVM;
using ahis.template.domain.Models.ViewModels.CommonVM;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AuditLogFeatures.Queries
{
    public class GetAuditLogQuery : IRequest<Result<PaginatedResult<AuditLogVM>>>
    {
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, Result<PaginatedResult<AuditLogVM>>>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        public GetAuditLogQueryHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<Result<PaginatedResult<AuditLogVM>>> Handle(GetAuditLogQuery query, CancellationToken cancellationToken)
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var pageNumber = Math.Max(query.PageNumber, 1);

            var auditLogs = _auditLogRepository.GetQueryable();


            if (!string.IsNullOrWhiteSpace(query.EntityName))
            {
                auditLogs = auditLogs.Where(a => a.EntityName == query.EntityName);
            }


            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                auditLogs = auditLogs.Where(a => a.EntityId == query.EntityId);
            }


            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                auditLogs = auditLogs.Where(a => a.UserId == query.UserId);
            }
                

            if (!string.IsNullOrWhiteSpace(query.Action) && Enum.TryParse<AuditActionEnum>(query.Action, ignoreCase: true, out var parsedAction))
            {
                auditLogs = auditLogs.Where(a => a.Action == parsedAction);
            }

            if (query.FromUtc.HasValue)
            {
                auditLogs = auditLogs.Where(a => a.TimestampUtc >= query.FromUtc.Value);
            }


            if (query.ToUtc.HasValue)
            {
                auditLogs = auditLogs.Where(a => a.TimestampUtc <= query.ToUtc.Value);
            }
                

            var totalCount = await auditLogs.CountAsync(cancellationToken);

            var items = await auditLogs
                .OrderByDescending(a => a.TimestampUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogVM
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.UserName,
                    UserRole = a.UserRole,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Action = a.Action.ToString(),
                    TimestampUtc = a.TimestampUtc,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    AffectedColumns = a.AffectedColumns,
                    IpAddress = a.IpAddress,
                    CorrelationId = a.CorrelationId
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<AuditLogVM>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
