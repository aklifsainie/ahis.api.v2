# Blueprint: RBAC foundation and first Superadmin bootstrap

**Status:** Implemented in source on 2026-09-29. `AddIdentityRoleBootstrapState` was generated and source-reviewed; it has not been applied to a database. This is the first of two P2 user-role-management blueprints.

## Outcome and business behavior

Establish a roles-only human-user authorization model, seed the two protected system roles, and promote one existing account to the first `Superadmin` through a durable one-time deployment bootstrap. This blueprint adds no public endpoint. The subsequent role-management API blueprint depends on this one.

The initial catalog is `Superadmin` and `User`, using the existing `IdentityRoleNames` constants. Both are protected from rename and deletion by the later API. Seeding does not assign `User` to new registrations or backfill existing users. Only `Superadmin` may read, manage, or assign human-user roles in v1. Human-user permissions and Identity role claims are outside this authorization model; API-client `api_permission` claims remain separate.

## Owning module, evidence, and flow

**Confirmed:** `Program.ConfigureAuthentication` registers `AddRoles<IdentityRole>()`; `IdentityContext` maps that role type and its user-role/role-claim relationships to the consolidated Identity role store. `AuthenticationService` copies current Identity role names into access-token claims. Bearer validation checks the user's security version and active session. `IIdentityTokenStateService.InvalidateAsync` rotates the security stamp and revokes refresh tokens and sessions. Existing admin actions use bearer `Superadmin` policies; step-up proofs are issued for five minutes by `AccountSecurityProofService`.

**Proposed flow:** API startup -> scoped Identity bootstrap service -> `RoleManager<IdentityRole>` for idempotent role creation -> `UserManager<ApplicationUser>` for confirmed, active target resolution and assignment -> Identity transaction for assignment, token invalidation, and durable completion marker. This stays in `ahis.template.identity`; it does not add an Application repository around Identity managers.

## Implementation and files

- Add an Identity-owned bootstrap service and options in `ahis.template.identity`, registered in `IdentityServiceRegistration`. Invoke it before the API begins serving requests. A single configuration key, `Identity:Bootstrap:SuperadminEmail`, identifies an **existing** account; it never creates a user, password, or token. Treat the email as deployment configuration, never print its value in startup logs.
- Add a fixed-key bootstrap-completion entity/table in `IdentityContext` with completion UTC timestamp and target user ID. Generate one `IdentityContext` migration, designer, and snapshot update through the approved migration workflow. The fixed primary key permits only one recorded first-administrator bootstrap. Do not modify earlier migrations.
- Seed exactly `Superadmin` and `User` through `RoleManager` at startup, using normalized-name lookups. Existing roles with those normalized names are reused; any incompatible or ambiguous store state stops startup for operator reconciliation. Seeding does not alter existing memberships or claims.
- When the configuration key is absent, leave the bootstrap marker and assignments untouched. When present and the marker exists, make no assignment; emit a generic operational notice to remove stale bootstrap configuration. When present and the marker is absent, require exactly one matching active, non-deleted, confirmed-email account that is not locked or under an effective restriction. If any `Superadmin` assignment already exists without the marker, stop startup and require reconciliation rather than promoting another user.
- Perform the first assignment, `IIdentityTokenStateService.InvalidateAsync` for that user, and marker insert in one serializable Identity transaction. Recheck marker and existing `Superadmin` assignments inside the transaction; a concurrent instance may lose the race and must roll back, then accept only the committed marker. Failed role-manager or token-state operations roll back all bootstrap effects and prevent startup. The target signs in again to obtain the role claim.
- Add dedicated `IdentityRoleReadPolicy`, `IdentityRoleManagementPolicy`, and `IdentityRoleAssignmentPolicy` in `ahis.template.api/Program.cs`. Each explicitly selects JWT bearer, requires an authenticated user, and requires `IdentityRoleNames.Superadmin`. The subsequent blueprint must attach these policies to its exact actions; no existing admin policy is broadened here.
- Update `docs/architecture/authentication-authorization.md`, the role-management backlog status/evidence, and deployment documentation after implementation. Preserve the existing user edits in the backlog.

## Authorization, security, and persistence

There is no bootstrap HTTP route, support backdoor, or automatic account creation. The configuration value must be supplied through a protected deployment channel and removed after successful bootstrap. The persisted marker, not removal of configuration, is the permanent one-time guard. Never log the address, credentials, security stamp, tokens, or raw proofs. A successful bootstrap audit should record a fixed event name, target ID, and UTC time; if explicit audit delivery uses `ApplicationDbContext`, attempt it after the Identity commit and do not change the committed result on audit failure.

The new table is an `IdentityContext` schema change. No application data schema, API response, dependency, notification, or API-client permission model changes. Applying the migration to any named database requires separate authorization and a pre-application inventory of migration history and role assignments. The user reported applying `ConsolidateIdentityRoleStore` to one database on 2026-09-29; other environments have not been verified.

## Tests and validation

- Unit/Identity tests: normalized idempotent seeding; no default `User` assignment; absent configuration; valid first bootstrap; stale configuration with marker; missing/unconfirmed/inactive/deleted/locked/restricted target; preexisting Superadmin without marker; concurrent contenders; role-manager or invalidation failure rolls back assignment and marker.
- Relational tests on a disposable SQL Server database: generated migration creates the marker with a unique fixed key, preserves role rows and assignments, and supports atomic bootstrap under concurrent startup. Review `Up`, `Down`, designer, and snapshot; `Down` must not silently erase evidence from a completed bootstrap.
- Policy tests: anonymous, API-key, and non-Superadmin principals are denied; bearer Superadmin is admitted; all three policies explicitly select bearer authentication.
- Verify narrow to broad after implementation: focused Identity/policy tests, `dotnet restore .\AhisApiTemplate.sln`, `dotnet build .\AhisApiTemplate.sln --no-restore`, then `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`. Never use `dotnet ef database update` for verification.

## Risks and assumptions

**Observed risk:** The consolidated role migration is source-reviewed but only user-reported applied to one database. Startup bootstrap cannot run against an unmigrated target; deployment order is migration first, then bootstrap configuration and application startup.

**Assumption:** A verified email identifies the intended existing user, but email access alone does not authorize bootstrap. Deployment control of the configuration and database is the trust boundary. If a Superadmin assignment already exists without a marker, an operator must inventory and reconcile it under a revised plan.

**Approval gate:** This blueprint changes authorization, startup behavior, configuration, and Identity schema. Approve this file revision before implementation; migration application and deployment require their own authorization.
