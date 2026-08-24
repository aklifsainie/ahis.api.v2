using ahis.template.domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.Entities
{
    public class AuditLog: BaseGuidEntity
    {
        // ---- WHO ----
        public string? UserId { get; set; } // nullable: anonymous submission, system jobs
        public string? UserName { get; set; } // snapshot at time of action, not joining to User table
        public string? UserRole { get; set; } // snapshot of role at time of action


        // ---- WHAT ----
        public string EntityName { get; set; } = default!;
        public string EntityId { get; set; } = default!; // stringified PK
        public AuditActionEnum Action { get; set; }

        // ---- WHEN ----
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        // ---- CHANGE DETAIL ----
        public string? OldValues { get; set; }        // JSON snapshot, sensitive fields masked
        public string? NewValues { get; set; }        // JSON snapshot, sensitive fields masked
        public string? AffectedColumns { get; set; }   // comma-separated list of changed property names

        // ---- CONTEXT ----
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? CorrelationId { get; set; }     // ties this row to HttpContext.TraceIdentifier

        // ---- OPTIONAL FREEFORM ----
        public string? Metadata { get; set; }          // JSON bag for anything action-specific (e.g. failure reason on LoginFailed)
    }
}
