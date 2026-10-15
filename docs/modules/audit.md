# Audit Module

Audit consists of transactional automatic mutation audits, best-effort explicit event/read audits, and paginated audit-log queries. It spans `AuditLogController`, `GetAuditLogQuery`, repository contracts and implementations, `AuditLogger`, `AuditSaveChangesInterceptor`, `AuditLog`, and `AuditLogVM`. Audit queries require bearer-authenticated `Superadmin`; API keys cannot read audit history.

Automatic audits are added to the same application context save and mask `[SensitiveData]`; key availability matters before save. Explicit audits obtain current request/actor context and suppress non-cancellation persistence errors. Audit pagination is newest-first and clamps page size to 1–100.

The audit query receives a repository `IQueryable` but runs EF operations in Application, an explicit architecture exception. See the local `AGENTS.md` and [auditing architecture](../architecture/auditing.md).
