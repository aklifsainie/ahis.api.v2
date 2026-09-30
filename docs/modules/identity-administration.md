# Identity Administration Module

## Ownership and boundaries

Identity Administration owns the human role catalog, custom-role lifecycle, permitted human-role claims, direct user-role assignment, and the one-time first-Superadmin bootstrap. Its Application entry point is `Features/IdentityRoleManagementFeatures`; its state and service boundary are in `ahis.template.identity`. It does not own API-client permissions, which remain separate API-key claims.

## Representative flows

Administrative controllers dispatch custom mediator requests to `IdentityRoleManagementHandler`, which delegates role, permission, and membership work to `IIdentityRoleManagementService`. That service uses `RoleManager`, `UserManager`, and `IdentityContext`; it owns protected-role checks, validation against `HumanPermissionCatalog`, optimistic concurrency, transactions, and invalidation of affected users' token/session state. The handler records best-effort redacted audit events only after material successful changes.

At API startup, `IIdentityRoleBootstrapService` ensures protected roles exist and may assign the first Superadmin only when its configured target and durable bootstrap state pass the service's checks. The bootstrap state belongs to `IdentityContext`; its migration must be generated and applied through the approved migration/deployment process, never as routine verification.

## Observed implementation invariants

- Read endpoints use the bearer-only Superadmin read policy. Role, permission, and user-role mutations additionally require the authenticated-security rate limit and a current actor-bound `X-Step-Up-Proof`.
- `Superadmin` and `User` are protected. Permission changes are restricted to the server-owned human permission catalog; API-client permission values are not interchangeable.
- Role rename and permission replacement use ETags. Material permission or membership changes invalidate affected users' credential/session state; no-op changes do not.
- Role-management mutations reject or fail according to the Identity service's current authorization, restriction, protected-role, and concurrency checks. These are observed implementation facts, not owner-confirmed product requirements.

## Required inspection and tests

Read the local `AGENTS.md`, `AdminIdentityController`, `AdminIdentityRolesController`, `AdminIdentityPermissionsController`, `IdentityRoleManagementRequests.cs`, `IdentityRoleManagementService`, `IdentityRoleBootstrapService`, `HumanPermissionCatalog`, `Program.cs`, `IdentityContext`, and the relevant migration/snapshot before planning a change.

`IdentityRoleManagementHandlerTest`, `IdentityRoleManagementServiceTest`, `IdentityPolicyTest`, and `IdentityConcurrencyTest` are representative coverage. The latter service/policy/concurrency paths use SQL Server LocalDB; follow the [testing strategy](../architecture/testing-strategy.md) and report unavailable prerequisites rather than substituting a database update.
