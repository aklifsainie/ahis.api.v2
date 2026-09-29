# Blueprint: Consolidate the Identity role store under `IdentityRole`

**Status:** Source implementation completed. The user reports applying `ConsolidateIdentityRoleStore` to their database on 2026-09-29; this report was not independently verified. Supersedes the table-name recommendation in `2026-09-27-rbac-role-store-consolidation.md`. Other database targets require separate inventory and authorization.

## Outcome and evidence

The configured `RoleManager<IdentityRole>` uses `AspNetRoles`, while an explicit `IdentityRole<string>` mapping created a separate `IdentityRoles` table. The initial Identity migration points `IdentityUserRoles` and `IdentityRoleClaims` to `AspNetRoles`. No committed role-creation endpoint or seed exists. The approved first phase makes the configured `IdentityRole` the sole mapped role entity and renames its operational table to the exact singular name `IdentityRole`. Role-management APIs and administrator bootstrap remain later RBAC work.

## Owning module and changes

- `ahis.template.identity` owns the model and its migration. Map `IdentityRole` to `IdentityRole` in `IdentityContext`, remove the explicit base-type mapping, and keep the API's `AddRoles<IdentityRole>()` registration.
- Generate `ConsolidateIdentityRoleStore` in `IdentityContext`, using `ahis.template.identity` as migration project and `ahis.template.api` as startup project. Rename `AspNetRoles` to `IdentityRole` instead of dropping and recreating it. Preserve `Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`, the unique filtered `RoleNameIndex`, and cascade relationships from user-role and role-claim tables. Rename the conventional primary-key and foreign-key constraints as scaffolded.
- Retire `IdentityRoles` only when empty. The migration must fail before any schema change if that table contains rows; populated data requires a separate reconciliation blueprint. Do not rewrite the initial migration. No public API, authorization policy, token behavior, dependency, or production configuration changes are included.

## Data, rollback, and deployment

Before applying the migration to any named environment, inspect its `__EFMigrationsHistory`, both role tables, assignments, claims, indexes, foreign keys, and external references. Confirm that the old migrations and tables exist as expected and that `IdentityRoles` is empty. The role-table rename preserves the operational rows and their IDs. The generated `Down` reverses the rename and restores an empty `IdentityRoles` table; it cannot recover rows previously removed from that table, so the empty-table precondition is mandatory. Apply only after separate authorization for the named database and an appropriate backup or recovery point. Do not seed `Superadmin` during this repair.

## Verification

Check that the EF model has only the configured role type mapped to `IdentityRole`, retains the role-name index, and points both role relationships to it. Inspect the migration's guard, rename, constraint operations, designer, and snapshot; test empty and populated duplicate-table cases on a disposable SQL Server database when available. Build the Identity project and solution, then run focused and solution tests. No live database access or migration application is implied by implementation approval.

## Assumptions and remaining decisions

The exact table name is singular `IdentityRole`. Live data and external writers have not been inspected. If `IdentityRoles` is populated, implementation of the schema repair may exist in source, but applying it must wait for a reviewed data plan. Role catalog, role assignment, bootstrap, and role-change token invalidation remain separate RBAC decisions.
