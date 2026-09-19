# Auditing

## Automatic change auditing

`AuditSaveChangesInterceptor` inspects added, modified, and deleted entities implementing `IAuditableEntity`. It adds `AuditLog` rows to the same `ApplicationDbContext`, so audit data and the business mutation commit together.

For modified records it records only changed scalar properties. `[SensitiveData]` values are replaced with a mask. `AuditLog` itself is excluded defensively.

Only mark an entity auditable deliberately. Confirm that its key is available when `SavingChanges` executes; database-generated keys can otherwise be recorded before their final value exists.

## Explicit event auditing

`IAuditLogger` is used for events that EF change tracking cannot represent, including reads and potential login/logout/export events. It captures actor/request context through `ICurrentUserService` and commits immediately. Failure is logged and suppressed except for caller cancellation.

Country GetAll and GetById are the current explicit-read examples.

## Audit queries

Audit-log queries support entity, entity ID, user, action, UTC range, and pagination filters. Page size is clamped to 1-100 and results are newest first. Audit rows intentionally have no foreign key to Identity users so history can survive user deletion.
