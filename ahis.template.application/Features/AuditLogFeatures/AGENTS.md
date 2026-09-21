# Audit Feature Instructions

Audit covers automatic mutation logging, explicit event/read logging, and paginated audit-log queries. It spans the Audit feature, Domain audit entity/view model, Infrastructure repositories/services/interceptor, and `AuditLogController`.

Before changing it, inspect `GetAuditLogQuery`, `AuditLogController`, `IAuditLogRepository`, `GenericGuidRepository`, `AuditLogger`, `AuditSaveChangesInterceptor`, and [the Audit module guide](../../../docs/modules/audit.md). The query exposes an `IQueryable` from Infrastructure and executes EF operations in Application; this is an established exception, not a default persistence boundary.

Automatic audits join the application save transaction; explicit audits suppress non-cancellation persistence failures. Audit access currently has no specific authorization policy. Treat changes to audit coverage, actor provenance, query visibility, or sensitive-field masking as security- and behavior-sensitive.
