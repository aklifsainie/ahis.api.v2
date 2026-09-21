# Auditing

`AuditSaveChangesInterceptor` inspects added, modified, and deleted `IAuditableEntity` instances and adds `AuditLog` rows to the same `ApplicationDbContext`; automatic audit data therefore participates in the database save transaction. It records changed scalar properties for modifications, masks `[SensitiveData]`, and excludes `AuditLog` itself.

Choose auditable entities deliberately. Their keys must be available when `SavingChanges` runs: Country uses a database-generated integer key, so create-audit IDs can be pre-generation values. A logical Country delete is an EF modification and is currently recorded as an update.

`IAuditLogger` records reads and other events that EF tracking cannot represent. It captures request/actor context and suppresses non-cancellation persistence failures, so explicit auditing is attempted rather than guaranteed. Country GetAll and GetById are current examples.

Audit queries filter by entity, entity ID, user, action, UTC range, and page; page size is clamped to 1–100 and results are newest first. Audit rows intentionally have no Identity foreign key, but audit access presently has no dedicated authorization policy.
