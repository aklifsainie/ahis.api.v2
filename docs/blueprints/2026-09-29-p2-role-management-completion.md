# Blueprint: complete P2 user role management (roles-only v1)

**Status:** Approved for implementation by the user on 2026-09-29. This blueprint completes the roles-only v1 work described in `2026-09-29-rbac-role-management-endpoints.md`. It does not authorize migration application, deployment, or changes to a user database.

## Outcome

Close the remaining non-optional P2 role-management gap: record added and removed role IDs in the redacted audit for a changed direct assignment set, and verify the existing security and transactional behavior. Reconcile the backlog with source and test evidence. Human-user permission endpoints remain optional, deferred work.

## Owning module and analogous implementation

**Confirmed:** `AdminIdentityRolesController` and the user-role actions in `AdminIdentityController` dispatch custom mediator requests to `IdentityRoleManagementHandler`. That handler delegates Identity operations to `IIdentityRoleManagementService`, implemented by `IdentityRoleManagementService` using `RoleManager<IdentityRole>`, `UserManager<ApplicationUser>`, and `IdentityContext`. `IAuditLogger` captures the current actor, timestamp, and request correlation context. Administrative account handlers show the existing post-commit, best-effort explicit audit pattern. Keep Identity persistence in the Identity service boundary; do not add an Application repository.

## Observed flow and proposed flow

**Confirmed:** `PUT /api/admin/identity/users/{userId}/roles` uses bearer `IdentityRoleAssignmentPolicy`, the authenticated-security limiter, and an actor-bound step-up proof. The service validates the desired role IDs, rejects self-assignment and removal of the last effective Superadmin, changes memberships in a serializable Identity transaction, and invalidates the target's token/session state. The handler currently audits every successful call with a fixed action and target user ID; the service returns no added/removed IDs or changed/no-op indication. The checked role catalog and assignment routes have no role-management relational or HTTP authorization tests in the committed suite. A baseline `dotnet test .\AhisApiTemplate.sln --no-restore` passed 51 tests on 2026-09-29.

**Proposed:** The Identity service computes the membership difference within its existing transaction and returns an internal change summary only after commit. The handler writes one best-effort audit for a changed set, including sorted added and removed role IDs. A no-op remains `204` without a mutation audit. Focused HTTP and relational tests verify the existing contract; any behavior discrepancy found by those tests is corrected within this blueprint's roles-only contract.

## Files to add, modify, or leave unchanged

- Modify `ahis.template.identity/Interfaces/IIdentityRoleManagementService.cs`, its implementation, and the Identity role-management DTOs to return an internal `IdentityRoleAssignmentChangeDto(bool Changed, IReadOnlyList<string> AddedRoleIds, IReadOnlyList<string> RemovedRoleIds)` from `ReplaceUserRolesAsync`.
- Modify `ahis.template.application/Features/IdentityRoleManagementFeatures/IdentityRoleManagementRequests.cs` to map that result back to the existing mediator `Result`, audit only a committed change, and serialize fixed action metadata with sorted role IDs. Keep actor and target identity from the existing request/audit context.
- Add role-management tests under `ahis.template.test/TestFeatures/IdentityFeature`. Add an API project reference and test-only `Microsoft.AspNetCore.Mvc.Testing` 8.0.22 package if required for HTTP authorization tests; use an isolated test host that bypasses production bootstrap and external integrations.
- Update `docs/backlog/identity-security-endpoints.md` after evidence is available: correct the stale Administrative Identity controls progress row, retain checked source-confirmed routes, mark the audit subitem complete only after verification, and state whether relational and endpoint tests passed. Reconcile the backlog's “approved” label with the existing endpoint blueprint's “proposed” status from an actual approval record; do not infer approval from implemented code. Update architecture/module documentation only if verified behavior differs from its current description.
- Leave public controllers, route names, request/response models, authorization policies, role claims, and Identity migrations unchanged unless a failing test demonstrates a defect within the existing approved v1 contract. Any material contract, schema, or security-policy change requires a revised blueprint.

## Business behavior and confidence

**Observed invariant:** Direct role replacement is an all-or-nothing desired-set operation. An unchanged set returns `204` without invalidating sessions. A changed set invalidates the target's access and refresh state. A user cannot replace their own roles or remove the last effective Superadmin. Protected `Superadmin` and `User` roles cannot be renamed or deleted.

**Proposed audit detail:** Return distinct added and removed role IDs, sorted with ordinal string ordering, from the transaction's computed difference. The audit metadata contains only a fixed action name and those ID arrays. Do not include role names, proof, bearer or refresh tokens, claims, security stamps, credentials, or profile data. Audit delivery remains best effort after commit and cannot turn a committed success into a failure.

## Authorization, security, and API contract

The eight existing endpoints, three dedicated bearer Superadmin policies, five-minute actor-bound step-up requirement on writes, authenticated-security rate limit, `If-Match` role-rename precondition, no-store responses, and current status/response shapes remain unchanged. The `PUT` user-role response stays `204` with no body. `IdentityRoleAssignmentChangeDto` is an internal service result, not a public API DTO. API-client `api_permission` claims remain a separate principal model; no human-user permission claim or arbitrary role-claim mutation is added.

## Entity, persistence, migration, and service impact

No entity, database schema, migration, production configuration, or runtime dependency change is planned. Preserve `IdentityContext`'s transaction and Identity-manager ownership. Compute the role-ID difference before the first membership write, return it only after a successful commit, and return `Changed = false` for a no-op. Propagate cancellation where the existing EF calls support it. Test-only host dependencies and LocalDB fixtures do not enter production projects.

## Transaction, audit, and integration impact

Keep membership changes and token/session invalidation in one Identity transaction. The audit uses `IAuditLogger` after commit; its existing request context supplies actor ID, UTC timestamp, and correlation ID, while the entity ID is the target user ID. A post-commit audit failure is logged without sensitive values and does not change the HTTP result. No notification or external integration is introduced. Existing role-create and no-op-rename audit semantics are observed separately and are outside this focused change unless testing exposes a contract-critical defect.

## Test plan

- HTTP tests: anonymous `401`, bearer non-Superadmin and API-key principal `403`, valid bearer Superadmin access, write proof rejection, assigned policy and rate-limit metadata, no-store headers, and redacted role/user responses. Verify status mapping for missing targets, protected or assigned roles, stale/missing ETags, and malformed desired sets at the relevant endpoint.
- Handler tests: one post-commit audit for a changed set with exact sorted added/removed IDs and target ID; no mutation audit for no-op or failed service result; audit failure does not reverse success; no sensitive values in metadata.
- Relational tests: create a unique disposable SQL Server LocalDB database from the current model for each isolated fixture and clean it afterward. Cover desired-set validation before writes, transaction rollback if membership or token invalidation fails, no-op behavior, protected-role behavior, assigned-role delete conflict, stale/racing role rename, affected-user invalidation, and two concurrent attempts to demote the last effective Superadmin. Assert that resulting membership and token/session state satisfy the existing contract. Do not connect to or apply migrations to a user database.
- Correct any test-proven defect only within the existing roles-only contract. If the fix requires a public contract, authorization, schema, dependency, or broader behavior change beyond this blueprint, stop and revise it before implementation.

## Documentation and validation

Run the new focused tests, then `dotnet restore .\AhisApiTemplate.sln`, `dotnet build .\AhisApiTemplate.sln --no-restore`, and `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`. Report test counts and LocalDB availability separately; a passing unit suite does not substitute for relational or HTTP authorization coverage. Update the backlog only with outcomes supported by source or test evidence.

## Risks, assumptions, and open questions

- **Confirmed risk:** JWTs snapshot role names. A missed token-state invalidation can preserve old authority; the relational tests must prove invalidation for membership changes and assigned-role renames.
- **Confirmed evidence gap:** The existing endpoint blueprint says “Proposed for explicit approval,” while the backlog progress record labels it approved. Reconcile this documentary mismatch without asserting an approval record that has not been found.
- **Assumption:** “Complete P2” means finish the roles-only v1 scope selected for this work. Optional permission endpoints, arbitrary role claims, and granular add/remove routes remain deferred and unchecked.
- **Assumption:** SQL Server LocalDB is available on the development host; `MSSQLLocalDB` was listed on 2026-09-29. Relational tests must fail or explicitly skip with a reported reason when it is unavailable, never silently substitute EF InMemory for concurrency or transaction assertions.
