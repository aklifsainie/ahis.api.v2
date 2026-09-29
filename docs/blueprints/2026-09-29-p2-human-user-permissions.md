# Blueprint: P2 human-user role permissions

**Status:** Approved by the user on 2026-09-30. It authorizes implementation, not migration application, deployment, or changes to a user database.

## Outcome

Add the optional P2 permission endpoints for human-user roles. The first server-owned catalog contains `country.read` and `country.write`. A custom role can grant these capabilities to bearer users without changing the existing `User` and `Superadmin` access rules or the separate API-client `api_permission` model.

## Owning module and analogous implementation

**Confirmed:** `AdminIdentityRolesController` dispatches role operations through custom `IMediator` requests in `IdentityRoleManagementFeatures` to `IIdentityRoleManagementService`. Its Identity implementation owns `RoleManager<IdentityRole>`, `UserManager<ApplicationUser>`, `IdentityContext`, step-up proof, transactions, token-state invalidation, and post-commit explicit audit. `IdentityContext` already maps `IdentityRoleClaims`. `AdminRoleDetailVM` already has a `Permissions` collection, currently populated as empty. `AuthenticationService.GenerateJwtTokenAsync` emits direct user claims and role names, but does not emit role claims. `Program.cs` defines Country read/write policies for bearer roles and API-client claims, and Country actions apply those policies.

## Observed flow and proposed flow

**Observed:** Role detail and mutation use the existing role controller → mediator → Identity service path. JWTs snapshot roles; token-state invalidation rotates the security version and revokes active refresh tokens and sessions. Country uses the `CountryAuthentication` selector, which chooses bearer when an Authorization header is present, otherwise an API key when present.

**Proposed:** Add permission GET/PUT actions to the role controller, matching mediator requests, and Identity service methods. Store only server-catalog permission codes as Identity role claims of type `human_permission`. Project allowed claims in role detail, issue their union in bearer JWTs, and extend Country policy assertions to recognize the bearer claim. Keep the Identity service as the persistence owner; add no Application repository or direct controller database access.

## Files to add, modify, or intentionally leave unchanged

- Add a small shared permission-code/claim-type definition accessible to Identity and API without an API-to-Identity dependency reversal; use it for catalog validation, JWT issuance, and policy checks. Add a public role-permissions view model and internal Identity DTOs as needed.
- Extend `AdminIdentityRolesController`, `IdentityRoleManagementRequests`, `IIdentityRoleManagementService`, `IdentityRoleManagementService`, and `AuthenticationService`. Extend `Program.cs` Country policy assertions and role-management tests.
- Update role-detail mapping to return actual allow-listed direct role permissions. Keep existing user-role assignment endpoints, role names, API-client claim type and policies, protected-role rename/delete behavior, and the Country authentication selector intact.

## Business behavior and confidence

**User-selected decisions:** The initial catalog is exactly `country.read` and `country.write`; either code grants only its named action. Permission grants from multiple directly assigned roles form a union. Existing `User` Country-read and `Superadmin` Country-read/write access remains. Only custom-role permission sets are mutable; protected `Superadmin` and `User` roles remain readable but return no managed permissions.

**Planned rules:** The PUT body is a complete desired set. Require a non-null array of at most two distinct, exact, case-sensitive catalog codes; an empty array clears permissions. Reject duplicate, unknown, blank, or malformed codes before writing. A no-op succeeds without session invalidation or mutation audit. On a change, invalidate all directly assigned users, including the actor if assigned, before commit; the actor must sign in again. Only role-derived, allow-listed `human_permission` claims grant human access. Existing arbitrary role claims and direct user claims do not grant a managed permission; exclude direct user claims of the reserved type when issuing JWTs.

## Authorization and security

Use the existing bearer-only `IdentityRoleReadPolicy` for GET and `IdentityRoleManagementPolicy` for PUT, with `AuthenticatedSecurityPolicy` on both. PUT requires the current actor-bound five-minute `X-Step-Up-Proof`; the Identity service rechecks current authoritative Superadmin membership before target lookup. Never accept actor identity from the body. Responses use `Cache-Control: no-store`. Country policy checks `human_permission` only on a bearer-authenticated human principal; API keys continue to use `api_permission`. A bearer Authorization header continues to take precedence over an accompanying API key.

## API contract

| Route | Success | Failure and precondition rules |
| --- | --- | --- |
| `GET /api/admin/identity/roles/{roleId}/permissions` | `200` with the existing `ResponseDto` wrapper around `{ permissions: string[] }`, sorted ordinally; opaque role ETag header | `404` for an unknown role; bearer policy failures retain existing `401`/`403` behavior |
| `PUT /api/admin/identity/roles/{roleId}/permissions` | `204` with no body and the resulting opaque ETag header; body `{ "permissions": ["country.read"] }` | `400` for missing/invalid proof or invalid body; `404` for unknown role; `409` for a protected role; `428` for missing `If-Match`; `412` for stale ETag or a detected concurrent role edit |

Use the same opaque role ETag as `GET /roles/{roleId}` and role rename. A permission change advances the role concurrency stamp, so a prior detail or permission ETag becomes stale; a no-op retains it. Existing role-detail responses now show the same sorted managed permission collection. Do not expose role claim IDs, claim types, concurrency stamps, direct user claims, or raw Identity entities. Rate limiting continues to return `429`.

## Entity, persistence, and migration impact

Use the existing `IdentityRoleClaims` table and `RoleManager<IdentityRole>` claim operations. No new entity, table, migration, runtime package, external integration, or production setting is planned. Store one `human_permission` role claim per assigned catalog code. Read and issue only allow-listed values. If pre-existing reserved-type claims contain unknown or duplicate values, PUT canonicalizes that role's reserved-type claims to the desired set; unrelated claim types remain untouched. Role ETag changes must use Identity's role concurrency update rather than an independent permission version.

## Service decision and evidence

Extend the dedicated Identity role-management service because it already coordinates proof checks, `RoleManager`, `IdentityContext`, assigned-user lookup, transactions, and token-state invalidation for role rename. Add service GET and full-set replacement methods and return an internal changed/added/removed summary to the mediator handler. The API controller remains a thin dispatcher; Application maps to public view models and attempts audit only after a committed change.

## Transaction, audit, and integration impact

For PUT, validate proof, actor, ETag, role, protected status, and desired codes before mutation. In one serializable Identity transaction, compare the current managed set, apply only the difference via Identity role-claim operations, advance role concurrency, invalidate every assigned user's security version and sessions, and commit. Roll back claims and invalidations together on failure. Preserve no-op behavior without token-state changes. Map concurrency conflicts to `412`; unexpected failures return a generic error without partial success. After a changed commit, attempt one best-effort target-role audit with fixed action and sorted added/removed permission codes. Do not audit proof, tokens, security stamps, user profiles, arbitrary claims, or credentials. Audit failure cannot reverse the committed response. No notification is introduced.

## Test plan

- HTTP tests: bearer Superadmin access; anonymous, non-Superadmin, and API-key denial; policy/rate-limit metadata; proof, `If-Match`, status mapping, no-store, response redaction, and role-detail consistency.
- Disposable SQL Server tests: full replacement, clear, no-op, invalid catalog input before writes, protected-role denial, stale/concurrent ETags, rollback on claim or user-invalidation failure, and invalidation of every assigned user. Confirm no unrelated role claims are changed.
- JWT and policy tests: issued allow-listed role-claim union across multiple roles; exclusion of direct-user and unknown role claims of the reserved type; `User` and `Superadmin` legacy Country access; custom-role read/write independence; API-key permission isolation; invalid bearer cannot fall back to an API key; old access and refresh sessions fail after a changed permission set.
- Handler/audit tests: exactly one redacted post-commit audit for a changed set, none for no-op or failure, and a successful HTTP result when audit delivery fails.

## Documentation impact

After implementation and verification, update the P2 backlog, `docs/architecture/authentication-authorization.md`, relevant module guidance, Swagger responses, and the role-management runbook. Record the user's reported manual roles-only runbook pass separately from the automated coverage still listed in the backlog. Do not mark permission work complete until the new routes and policy behavior are verified.

## Validation commands

Run focused permission tests first, then from the repository root:

```powershell
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

Use only disposable test databases for relational tests. Do not apply a migration or mutate a user database as verification.

## Risks, assumptions, and open questions

**Risk:** JWTs contain an authorization snapshot. Failing to invalidate any assigned user after a changed claim could preserve old authority. Concurrency and rollback tests must prove the claimed atomicity, including when multiple administrators edit the same role.

**Assumption:** The deployed Identity schema includes the existing role-claims table and the role-store consolidation already required by roles-only P2. Verify target-environment migration state before deployment; this blueprint does not authorize migration application.

**Open decisions:** None for the first release. Expanding the catalog, changing protected-role permissions, adding direct user grants, or applying human permissions to other endpoints requires a separate reviewed change.
