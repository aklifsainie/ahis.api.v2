using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Common;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Persistences.Interceptors
{
    /// <summary>
    /// Hooks into EF Core's SaveChanges pipeline. Before changes hit the DB, it
    /// inspects the ChangeTracker for any entity implementing IAuditableEntity,
    /// builds an AuditLog row per change, and adds it to the same DbContext so it
    /// commits in the SAME transaction as the business data change.
    ///
    /// IMPORTANT: entities audited this way must generate their PK client-side
    /// (e.g. `public Guid Id { get; set; } = Guid.NewGuid();`) so the Id is known
    /// BEFORE SaveChanges runs. DB-generated identity columns won't have a value
    /// yet at this point in the pipeline.
    /// </summary>
    /// 

    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        // Add entity type names here that should NEVER be audited even if they
        // accidentally implement IAuditableEntity (defensive safety net).
        private static readonly HashSet<string> ExcludedEntities = new()
        {
            nameof(AuditLog)
        };

        public AuditSaveChangesInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                AddAuditEntries(eventData.Context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
            {
                AddAuditEntries(eventData.Context);
            }

            return base.SavingChanges(eventData, result);
        }

        private void AddAuditEntries(DbContext context)
        {
            // Snapshot the list first - we're about to add new AuditLog entries to
            // the tracker, and we don't want to iterate over those too.
            var entries = context.ChangeTracker
                .Entries<IAuditableEntity>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            if (entries.Count == 0) return;

            var auditLogs = new List<AuditLog>();

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;
                if (ExcludedEntities.Contains(entityName)) continue;

                // Skip properties that don't map to actual columns (e.g. navigation properties)
                var scalarProperties = entry.Properties.ToList();

                var affectedColumns = new List<string>();
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var prop in scalarProperties)
                {
                    var propertyName = prop.Metadata.Name;
                    var isSensitive = IsSensitiveProperty(entry, propertyName);

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propertyName] = isSensitive ? "***MASKED***" : prop.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            oldValues[propertyName] = isSensitive ? "***MASKED***" : prop.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                            {
                                affectedColumns.Add(propertyName);
                                oldValues[propertyName] = isSensitive ? "***MASKED***" : prop.OriginalValue;
                                newValues[propertyName] = isSensitive ? "***MASKED***" : prop.CurrentValue;
                            }
                            break;
                    }
                }

                // For Modified entities with no actual changed values (rare, but can
                // happen with concurrency tokens etc.), skip writing a no-op audit row.
                if (entry.State == EntityState.Modified && affectedColumns.Count == 0)
                    continue;

                var action = entry.State switch
                {
                    EntityState.Added => AuditActionEnum.Create,
                    EntityState.Modified => AuditActionEnum.Update,
                    EntityState.Deleted => AuditActionEnum.Delete,
                    _ => AuditActionEnum.Update
                };

                var entityId = GetPrimaryKeyValue(entry);

                auditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = _currentUserService.UserId,
                    UserName = _currentUserService.UserName,
                    UserRole = _currentUserService.UserRole,
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = action,
                    TimestampUtc = DateTime.UtcNow,
                    OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                    AffectedColumns = affectedColumns.Count > 0 ? string.Join(",", affectedColumns) : null,
                    IpAddress = _currentUserService.IpAddress,
                    UserAgent = _currentUserService.UserAgent,
                    CorrelationId = _currentUserService.CorrelationId
                });
            }

            if (auditLogs.Count > 0)
            {
                context.Set<AuditLog>().AddRange(auditLogs);
            }
        }

        private static bool IsSensitiveProperty(EntityEntry entry, string propertyName)
        {
            var clrProperty = entry.Entity.GetType().GetProperty(propertyName);
            return clrProperty?.GetCustomAttributes(typeof(SensitiveDataAttribute), inherit: true).Any() ?? false;
        }

        private static string GetPrimaryKeyValue(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is null) return "UNKNOWN";

            var values = key.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null");

            return string.Join(",", values);
        }
    }
}
