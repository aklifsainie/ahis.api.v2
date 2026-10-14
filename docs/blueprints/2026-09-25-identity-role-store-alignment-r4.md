# Blueprint: Identity role-store alignment and initial role seeding — revision 4

**Status:** Proposed for review. This is a new revision; preserve `2026-09-25-identity-role-store-alignment.md` and the administrative-control drafts as historical context. Approval of this revision does not authorize database access, migration generation or application, or deployment.

## Outcome and scope

Align the ASP.NET Core Identity runtime role store with the `IdentityContext` model and seed an initial catalog containing exactly two protected system roles: `Superadmin` and `User`. A protected role must not be renamed or deleted by later role-management work. This change creates no user-role assignment, first-administrator bootstrap path, role-management endpoint, or role claim. Existing roles and assignments, if any, must be preserved; their existence is unknown until separately authorized discovery.

This blueprint establishes a fail-closed prerequisite. The owner has deferred all appsettings/configuration work and all database discovery to a later manual step. No implementation edit or migration design may proceed from an assumed live database state. Place all further P2 administrative Identity-control work, including disable, enable, reset-MFA, and require-password-reset, on hold until this prerequisite is completed and approved. Do not alter their code or contracts under this blueprint. Existing administrative routes also require role-store verification before any deployment decision.

## Owning module, analogous implementation, and source evidence

Identity owns the role entity, EF context, migration, `RoleManager<IdentityRole>`, and `UserManager<ApplicationUser>` behavior. The API owns Identity registration and authorization policies. Authentication issues JWT role claims. Account's Identity service is the closest pattern for multi-step Identity work; the custom mediator remains the API-to-Application path for feature handlers. Country's repository/unit-of-work pattern, API-client direct-service variation, and the audit query's Application-side EF execution are not patterns for role persistence.

**Confirmed source observations:** `ahis.template.api/Program.cs` registers `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<IdentityContext>()`. `IdentityContext : IdentityDbContext<ApplicationUser>` separately maps `IdentityRole<string>` to `IdentityRoles`. The checked-in model snapshot contains both the concrete `IdentityRole` mapped to `AspNetRoles` and `IdentityRole<string>` mapped to `IdentityRoles`. The initial Identity migration creates both tables; its `IdentityUserRoles.RoleId` and `IdentityRoleClaims.RoleId` foreign keys target `AspNetRoles`. `AuthenticationService` calls `UserManager.GetRolesAsync` and emits each returned name as a `ClaimTypes.Role` JWT claim. `Program.cs` policies require `IdentityRoleNames.Superadmin`, and `AccountService` checks current Superadmin membership for an administrative action. `IdentityRoleNames` already defines `Superadmin` and `User`.

**Inference from source, not a database fact:** the configured concrete runtime role type and the extra base-type mapping can cause EF seeding directed at `IdentityRoles` to miss the role store used for authorization. Source migrations and snapshots do not establish which migrations were applied, which tables exist in any target database, or where live roles, assignments, and claims reside. The prior draft reports a discarded trial migration; that report is historical context, not evidence of target database state.

## Observed and proposed flow

Current source path: Identity registration → EF role store → `IdentityContext`; login → `UserManager.GetRolesAsync` → `ClaimTypes.Role` in JWT → bearer validation → `RequireRole(Superadmin)` on applicable administrative actions. The proposed path keeps these boundaries and makes one authoritative role entity/table serve runtime lookup, assignments, claims, and the two-role seed. It does not change request or response contracts.

## Fail-closed manual database-discovery prerequisite

A separately authorized owner must inspect each named target environment before selecting a mapping or writing a migration. Record only non-sensitive, reviewable evidence in a restricted discovery report, without connection strings, credentials, raw database rows, user identifiers, tokens, claims, or configuration values. Required facts are:

1. The environment identifier and applied Identity migration identifiers from `__EFMigrationsHistory`, including whether the initial and later checked-in Identity migrations are applied. Record identifiers only, not connection details.
2. Existence and schema of `AspNetRoles`, `IdentityRoles`, `IdentityUserRoles`, and `IdentityRoleClaims`: columns, types, nullability, primary keys, unique indexes, foreign-key targets, cascade behavior, and relevant collation/case behavior.
3. Aggregate role counts per candidate table; whether normalized `SUPERADMIN` and `USER` already exist in either table; duplicate or conflicting normalized names; and whether the stored names meet the intended exact catalog spelling. Report counts and conflict classifications, not row values or role IDs.
4. Aggregate assignment and role-claim counts grouped by referenced role table; counts of orphaned relationships and any references to duplicate/conflicting role names. Provide a redacted relationship diagram or metadata report, not user or claim data.
5. Whether other application code, jobs, or approved migrations write either role table or depend on its name; whether any pending migration would change these objects. Record source references and migration identifiers, not configuration.
6. Availability of an approved backup/restore and maintenance plan for the named environment, with verification responsibility and a recovery point. Record readiness only, not backup contents or locations containing sensitive details.

If discovery is missing, inconsistent, reveals unexpected relationships, or cannot establish how assignments and claims are preserved, stop. Do not generate a seed migration, change mappings, drop/rename a table, apply a migration, or deploy role-dependent controls. A material finding requires a new dated blueprint revision and approval before implementation.

## Mapping alternatives and design decision

Select and document one alternative only after discovery:

- **`AspNetRoles` is authoritative:** retain the concrete `IdentityRole` runtime mapping and the existing assignment/claim foreign-key target; remove or correct the extra base-type mapping. A generated drop of `IdentityRoles` is acceptable only if discovery proves it contains no roles or references and the owner explicitly approves the data effect. Otherwise preserve and reconcile it under a reviewed data plan.
- **`IdentityRoles` is authoritative:** map the concrete runtime `IdentityRole` to that table and design a reviewed transfer or foreign-key repair for assignments and claims. Existing keys, role identity, normalization, and referential integrity must survive. The fact that a table has this name is insufficient evidence to choose it.
- **Both contain meaningful or conflicting data:** design an explicit reconciliation, including stable-ID mapping, name conflicts, dependent rows, verification, and rollback. No automatic destructive rename, merge, or cascade is permitted.

The preferred outcome is one canonical runtime role table and no duplicate EF role entity. The table choice and data-operation sequence remain open decisions until manual evidence is reviewed. Keep existing role rows that are outside the two-role initial seed unless a separately approved scope addresses them; “initial catalog” does not authorize deleting live roles.

## Seed and protected-role behavior

Seed exactly `Superadmin` and `User`, with normalized names consistent with Identity's normalizer. Check for existing normalized-name matches and preserve their IDs and dependent rows. If there is a conflicting name or ambiguous match, fail before mutation and return to design review. Choose a deterministic, repeatable migration seed method after the canonical table is known; review how role IDs and concurrency values are supplied. Do not use an implicit startup bootstrap or unreviewed `HasData` changes that rewrite existing roles. No user is assigned either role. Protection means future administrative role-management designs must prohibit deletion and unsafe rename of these two names; this blueprint does not add a role-management API or claim that the current code already enforces that protection.

If no user already has `Superadmin`, role-dependent administrative routes will remain inaccessible after seeding. That is an expected gate, not permission to create an assignment or bootstrap flow here. Any first-administrator procedure requires its own design and approval.

## Files and modules

- **Likely modify after gates:** `ahis.template.identity/Contexts/IdentityContext.cs` for the chosen concrete-role mapping; a new `ahis.template.identity/Migrations/<timestamp>_*.cs`, designer, and `IdentityContextModelSnapshot.cs` after separate migration-generation approval.
- **Review; modify only if the approved mapping requires it:** `ahis.template.api/Program.cs` Identity registration or policy wiring; `ahis.template.identity/Security/IdentityRoleNames.cs` already contains the two canonical names and normally needs no edit.
- **Add focused tests in an appropriate Identity/authorization test project or test host:** relational role-store mapping and migration behavior, manager lookup and assignment reads, JWT role claims, and policy decisions. Test infrastructure/dependencies need their own approval if new packages or projects are required.
- **Documentation after implementation:** `docs/architecture/authentication-authorization.md`, `docs/architecture/persistence.md`, `docs/backlog/identity-security-endpoints.md`, and affected Identity module guidance. Record the selected table, migration status, protected catalog, and P2 hold/release status without database data.
- **Intentionally unchanged in this scope:** appsettings and all configuration; controllers, public contracts, P2 administrative action implementations, user-role assignments, bootstrap flows, API-client authentication, ApplicationDbContext, and audit persistence. Preserve earlier blueprint revisions.

## Authorization, API, transactions, and audit

There is no new endpoint or public API contract. Existing policies that require `Superadmin` and JWT role claims must be verified against the corrected store; a configured policy alone does not prove an action enforces it. The role seed grants no access by itself. Verify a fresh token for an authorized existing member and denial for a principal without that role, including bearer-scheme behavior at protected actions. JWTs carry role claims captured at issuance, so any later role-membership change needs a separate token invalidation decision; this blueprint makes no membership change. Keep human Identity roles separate from API-client permissions.

Any data repair and seed must have explicit transaction boundaries, preconditions, affected-row checks, and postconditions. Failure before commit must leave the prior role and relationship state intact. This prerequisite performs no audit write or external integration. If later implementation introduces an operational security event, design a redacted audit separately; never record credentials, tokens, raw claims, or sensitive row values.

## Migration, rollback, and deployment gates

1. **Blueprint review:** approve this exact saved revision as a planning artifact. This does not authorize implementation or database discovery.
2. **Database discovery:** obtain separate authorization for manual, read-only inspection of each named environment. Submit the non-sensitive evidence above. Appsettings/configuration work remains deferred.
3. **Migration design:** choose the canonical table and write a revised, evidence-backed operation and preservation plan. Obtain explicit approval for any schema, data, or security effect before editing the model. Define conflict handling, preflight checks, transaction behavior, postchecks, and rollback.
4. **Migration generation:** obtain separate explicit approval to generate an `IdentityContext` migration using `ahis.template.identity` as the migration project and `ahis.template.api` as startup project. Generation is not application. Inspect every generated `Up`, `Down`, designer, snapshot, and SQL script. Reject unintended drops, broad data updates, assignment/claim loss, or seed inserts into the wrong table.
5. **Migration application:** require separate explicit approval for the named environment, reviewed script, backup/restore readiness, execution window, and operator. Do not use `dotnet ef database update` as routine verification. Capture non-sensitive pre/post aggregate checks for both roles, assignments, role claims, indexes, and foreign keys. Stop on any mismatch.
6. **Deployment:** require separate explicit authorization after migration verification, application compatibility checks, JWT/policy tests, and a reviewed recovery plan. Keep P2 administrative Identity controls on hold until completion and approval of this prerequisite; their release requires their own applicable approvals.

Rollback must preserve existing role IDs, user-role relationships, claims, and unrelated Identity data. A generated `Down` that drops a populated table or cascades dependent rows is not an acceptable automatic recovery path. Define whether the safe route is a compensating migration or a verified database restore before application. If data was reconciled, document how it can be restored without losing intervening changes. Never infer rollback safety from EF reversibility alone.

## Test and validation plan

After implementation is authorized, run narrow to broad: inspect the EF model for one concrete role mapping and correct user-role/role-claim foreign keys; exercise `RoleManager<IdentityRole>` lookup for both seeded names and `UserManager<ApplicationUser>.GetRolesAsync` against a disposable relational database; verify the migration on empty and representative preserved-data fixtures, conflict failure, repeated deployment behavior, and rollback limits. Verify JWT role-claim issuance for an already assigned test user and authorization outcomes for assigned, unassigned, and stale-token cases at the actual administrative actions. Tests may create fixture assignments only in isolated test storage; production seeding assigns no users. Review generated SQL and compare non-sensitive pre/post counts. Then run focused affected-project checks, solution build, and tests with concise output. Restore only if required. No build, test, migration, or database command is authorized by this planning task.

## Risks, assumptions, and open decisions

**Risks:** seeding the unused table leaves policies ineffective; a mistaken table drop or key rewrite can lose roles, assignments, or claims; normalized-name conflicts can duplicate system roles; existing JWT claims can outlive role changes; existing administrative code may be deployed before its role prerequisite is valid.

**Assumptions:** the owner-confirmed initial catalog is exactly protected `Superadmin` and protected `User`; no assignment or bootstrap is authorized; all target database facts are unknown. Source migration definitions describe intended schema, not actual deployment state.

**Open decisions, gated on manual discovery:** canonical table; treatment of the extra table and any meaningful rows; exact seed mechanism and stable identifiers; environment-specific migration sequence and rollback method; test fixture/provider setup; and operational release of role-dependent P2 controls. Resolve these in a new reviewed revision if evidence changes scope or design. No appsettings/configuration decision is part of this blueprint.