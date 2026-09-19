# Persistence

## Contexts

`ApplicationDbContext` in Infrastructure owns:

- `Country`
- `ApiClients`, `ApiClientKeys`, and `ApiClientPermissions`
- `AuditLog`

It discovers `IEntityTypeConfiguration<T>` implementations and installs `AuditSaveChangesInterceptor`.

`IdentityContext` in Identity owns:

- ASP.NET Core Identity users, roles, claims, logins, and tokens
- `RefreshTokens`

Both contexts use SQL Server and the same configured connection string, but have separate migrations and snapshots.

## Repository and unit-of-work behavior

- `GenericRepository<T>` supports `BaseEntity` integer-key records.
- `GenericGuidRepository<T>` supports `BaseGuidEntity` records and exposes `GetQueryable` for audit pagination.
- Generic reads normally exclude `IsDelete`; Country active reads add `IsActive` predicates.
- Reads default to no tracking. Updates and soft deletes load tracked records.
- Application mutations save through `IUnitOfWork` in the repository pattern.
- Identity services use `IdentityContext`, Identity managers, and `IdentityUnitOfWork` directly.

## Migrations

Application migrations belong to `ahis.template.infrastructure/Migrations` with `ApplicationDbContext`. Identity migrations belong to `ahis.template.identity/Migrations` with `IdentityContext`. Before generating a migration, produce an approved migration blueprint naming context, startup project, entities, tables, columns, indexes, foreign keys, destructive risks, and migration name.

Never apply a migration to a database without explicit instruction.

## Known cautions

- The initial Identity migration contains both default and renamed role-table artifacts; inspect the model snapshot and generated migration before future Identity migrations.
- The audit interceptor records keys before save, while Country uses a database-generated integer key.
- No global EF query filters are configured; repository predicates enforce soft deletion.
