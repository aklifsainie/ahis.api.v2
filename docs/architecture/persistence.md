# Persistence

`ApplicationDbContext` in Infrastructure owns Country, API-client, and audit data. It discovers entity configurations and installs `AuditSaveChangesInterceptor`. `IdentityContext` in Identity owns ASP.NET Core Identity data, `RefreshSessions`, and hash-only `RefreshTokens`. Both use SQL Server and the same configured connection-string key, but have separate migration histories and snapshots.

`GenericRepository<T>` supports integer-key `BaseEntity` records; `GenericGuidRepository<T>` supports `BaseGuidEntity` records and exposes `IQueryable` for audit pagination. Generic reads normally filter `IsDelete`; Country handlers add `IsActive`. Repository-pattern mutations use `IUnitOfWork`; Identity services work through Identity managers/context/unit of work.

No global EF query filter is configured. Soft-deletion behavior is enforced by repository predicates, and Country's unfiltered unique indexes retain soft-deleted values. Treat the resulting pre-check/database conflict as an observed risk.

Application migrations belong to `ahis.template.infrastructure/Migrations` with `ApplicationDbContext`; Identity migrations belong to `ahis.template.identity/Migrations` with `IdentityContext`. Generate migrations only through the repository migration skill and an approved migration blueprint. Never apply one without separately explicit authority.

Known observations: the initial Identity migration contains default and renamed role-table artifacts; Country's database-generated ID may be unavailable to the audit interceptor before save; and audit query pagination leaks EF work into Application.
