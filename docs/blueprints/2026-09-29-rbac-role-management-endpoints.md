# Blueprint: role catalog and user-role management endpoints

**Status:** Proposed for explicit approval. This is the second P2 user-role-management blueprint and depends on `2026-09-29-rbac-foundation-bootstrap.md`. Approval applies only to this file revision.

## Outcome and scope

Expose the five role-catalog and three user-role-assignment endpoints in the P2 backlog. The v1 model authorizes human users by Identity roles only. `Superadmin` and `User` are protected system roles. Only a current `Superadmin` may access the endpoints; writes require the existing five-minute actor-bound step-up proof. Role permissions, arbitrary claim editing, granular add/remove assignment routes, and API-client permissions are separate work.

## Owning module and observed flow

**Confirmed:** `AdminIdentityController` dispatches through custom `IMediator` requests to Account handlers, then `IAccountService` and Identity managers/context. Its mutation actions use dedicated bearer Superadmin policies, `AuthenticatedSecurityPolicy`, `X-Step-Up-Proof`, and redacted post-commit audits. `AccountSecurityProofService` validates proof against actor ID and current security version. `IIdentityTokenStateService.InvalidateAsync` rotates that version and revokes all active refresh tokens/sessions. `AuthenticationService` snapshots role names into JWTs. `IdentityContext` owns roles and user-role links; it already has a consolidated role-store migration.

**Proposed flow:** role/user admin controller -> custom mediator query/command -> dedicated `IIdentityRoleManagementService` -> `RoleManager<IdentityRole>`, `UserManager<ApplicationUser>`, `IdentityContext`, proof/token-state services -> Identity transaction where needed -> response and explicit audit. Keep Identity operations in the Identity service boundary and use no Application repository wrapper.

## API contract and authorization

Attach `IdentityRoleReadPolicy` to reads, `IdentityRoleManagementPolicy` to role writes, and `IdentityRoleAssignmentPolicy` to assignment writes; all three are the bearer Superadmin policies from the foundation blueprint. Apply `AuthenticatedSecurityPolicy` to every action. Derive the actor ID from `ICurrentUserService`; never accept it from request data. Validate write proof from `X-Step-Up-Proof` before role/user target lookup and recheck current authoritative Superadmin membership in the Identity service. Set `Cache-Control: no-store` for all responses. Missing/invalid bearer is `401`; non-Superadmin or API-key principals are `403`; missing/invalid proof is a generic `400`; rate limiting is `429`.

| Route | Success contract | Rules |
| --- | --- | --- |
| `GET /api/admin/identity/roles` | `200` paged `{ id, name, isProtected, assignmentCount }` | `pageNumber=1`, `pageSize=20`, clamp to 1–100; optional `name` filter normalizes via `RoleManager` and matches normalized-name prefix; order by normalized name then ID. |
| `GET /api/admin/identity/roles/{roleId}` | `200` `{ id, name, isProtected, assignmentCount, permissions: [] }` | `404` for unknown ID. The empty permissions collection documents that v1 grants no human-user role permissions; do not project arbitrary stored claims. Include an opaque `ETag`. |
| `POST /api/admin/identity/roles` | `201` with role detail and `Location` to detail route | Body `{ name }`; proof required; reject blank, normalized duplicate, reserved, or invalid name. |
| `PUT /api/admin/identity/roles/{roleId}` | `200` updated role detail with new `ETag` | Body `{ name }`; proof and `If-Match` required; `428` when absent, `412` when stale, `404` unknown; protected-role names cannot change. |
| `DELETE /api/admin/identity/roles/{roleId}` | `204` | Proof required; `404` for missing or repeated deletion; `409` for protected or assigned roles. No implicit reassignment. |
| `GET /api/admin/identity/users/{userId}/roles` | `200` direct assignments `{ id, name, isProtected }[]` | `404` for unknown user; no inherited/effective access expansion. Order by normalized role name then ID. |
| `PUT /api/admin/identity/users/{userId}/roles` | `204` | Body `{ roleIds: string[] }` is the complete desired direct set; proof required; reject duplicates, unknown IDs, or more than 100 IDs; no partial update. |
| `GET /api/admin/identity/roles/{roleId}/users` | `200` paged `{ userId, username }` | `404` unknown role; page defaults/max as above; order by user ID. No email, security state, claims, or full Identity user entity. |

Custom role names are trimmed, 1–64 characters, and contain only letters, digits, spaces, hyphens, or underscores. Use `RoleManager` normalization/validation and the unique normalized-name index; compare protected names case-insensitively. A no-op update with a matching ETag succeeds without invalidating users. Encode the ETag as a quoted, opaque hash of role ID and current `ConcurrencyStamp`; do not expose the stamp in JSON. A stale precondition or Identity concurrency failure returns `412`; a duplicate normalized name returns `409`. Other malformed inputs return `400`. `GET /roles` should also emit item ETags only if the contract deliberately adds them; v1 uses detail GET as the edit preflight.

## Business invariants and transaction design

- Prohibit all caller changes to their own role set, including no-op PUTs. Role creation/rename/deletion remains allowed because it does not directly change the actor's membership; a custom role assigned to the actor cannot be renamed unless the actor's authorization state can be safely invalidated after commit. Protected roles cannot be renamed/deleted.
- An effective Superadmin is directly assigned `Superadmin`, active, non-deleted, not currently locked, and allowed by restriction provenance. Reject removal of the last effective Superadmin. Evaluate this inside the same serializable transaction as assignment changes, with database serialization of competing Superadmin membership writes; test two concurrent demotions. Reject assignment to an inactive, deleted, locked, or restricted target until an approved recovery/admin workflow makes them eligible.
- For PUT assignments, validate the actor/proof, target, all role IDs, and desired set before writes. Compute added/removed memberships, apply via `UserManager`, call `IIdentityTokenStateService.InvalidateAsync(target)`, and commit atomically. If the set is unchanged, return `204` without invalidation or mutation audit. A changed set ends all target sessions; the target must sign in again.
- Renaming an assigned custom role changes the role name embedded in future JWTs. In one Identity transaction, update with Identity optimistic concurrency and invalidate every assigned user's token/session state before commit. If any invalidation fails, roll back the rename. Deletion is allowed only when a locked transactional check confirms no assignments. Creation and deletion use `RoleManager` and the same protected-name checks.
- Handle database uniqueness, foreign-key, and concurrency races as controlled `409`/`412` responses; unexpected failures before commit return a generic `500`. Avoid reporting success after a partial Identity mutation. Cancellation propagates through supported EF calls; post-commit audit delivery cannot undo success.
- After each committed change, attempt a redacted audit with actor ID from request context, target role/user ID, fixed action name, UTC time, request correlation ID, and added/removed role IDs for membership changes. Never include proof, bearer/refresh token, cookie, security stamp, raw claim values, credential material, or full user profile. Audit failure remains best effort and is logged without sensitive values.

## Files, service decision, and persistence impact

- Add an admin role controller in `ahis.template.api/Controllers/v1` and extend the existing admin user controller with user-role actions. Add custom mediator requests/handlers under a dedicated Identity role-management feature folder in Application, plus narrow Domain response view models; register handlers using the existing mediator discovery pattern.
- Add `IIdentityRoleManagementService`, implementation, and Identity DTOs in `ahis.template.identity`; register the service in `IdentityServiceRegistration`. It owns all `RoleManager`, `UserManager`, `IdentityContext`, transaction, proof, and token-state coordination. Keep `IAccountService` focused on account-security workflows.
- Reuse the existing `IdentityRole`, user-role join table, Identity transaction support, and token-state service. No new schema, migration, dependency, external integration, or production configuration is planned by this blueprint. If relational verification reveals a schema need, stop and revise the blueprint.
- After implementation, update `docs/backlog/identity-security-endpoints.md`, `docs/architecture/authentication-authorization.md`, the Account module guide as applicable, and Swagger action summaries/statuses. Do not overwrite unrelated or existing user edits in the backlog.

## Tests and validation

- Handler/controller tests: exact route, policy, rate-limit, proof header, status mapping, response redaction, actor ID provenance, bounded pagination/filtering, and API-key denial. Cover `401`, `403`, `400`, `404`, `409`, `412`, `428`, and `429` at the appropriate endpoint.
- Identity relational tests: normalized duplicate/reserved names; protected rename/delete; assigned-role delete conflict; ETag stale/racing updates; desired-set validation before writes; transaction rollback on membership or invalidation failure; self-assignment denial; zero/no-op set; last effective Superadmin under concurrent demotion; inactive/restricted target; role rename invalidating every affected user; bearer and refresh denial after a membership/role-name change; audit redaction and post-commit failure.
- Use a disposable SQL Server database for transaction, `RoleManager`, and concurrency behavior; mocks alone do not prove atomicity. Run focused tests, then `dotnet restore .\AhisApiTemplate.sln`, `dotnet build .\AhisApiTemplate.sln --no-restore`, and `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`. Do not apply migrations to a user database as verification.

## Risks, assumptions, and approval

**Dependency:** Deploy the approved foundation migration and successfully complete first-administrator bootstrap before relying on these routes. The consolidated role store was reported applied to one database, not independently verified across environments.

**Known risk:** Existing JWTs contain a role snapshot. Every membership change and assigned-role rename must invalidate each affected user's security version and refresh sessions within the Identity transaction; policy checks alone do not make old JWTs current.

**Assumptions:** Role IDs and user IDs are opaque strings; v1 has no tenant boundary or inherited permissions. An existing arbitrary Identity role claim does not grant human-user permission in this plan. Role-read responses show direct assignment state only. Approval of this file covers the listed public routes, authorization, and business rules; permission endpoints need their own future blueprint.
