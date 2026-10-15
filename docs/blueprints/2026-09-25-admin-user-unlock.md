# Blueprint: administrative unlock of an Identity user

**Status:** Proposed for approval. This blueprint covers only `POST /api/admin/identity/users/{userId}/unlock`. Approval applies to this saved revision. The backlog tracks the work but does not approve implementation.

## Outcome and scope

Add a human Superadmin action that clears an active ordinary Identity lockout for another active, non-deleted user. A committed unlock clears `LockoutEnd`, resets `AccessFailedCount` to zero, preserves `LockoutEnabled = true`, invalidates the target's access tokens, refresh tokens, refresh sessions, and step-up proofs atomically, attempts an actor/target audit, and sends a best-effort notification to the target. Unlock never reactivates or restores an account.

The owner has approved the authorization, eligibility, idempotency, notification, and HTTP behavior below. There are no remaining endpoint business decisions for the owner.

## Owning module and analogous implementation

The existing administrative route lives in `ahis.template.api/Controllers/v1/AdminIdentityController.cs`. Account commands use the custom mediator in `ahis.template.application/Features/AccountFeatures/Commands`; Identity operations stay behind `IAccountService` in `ahis.template.identity/Interfaces/IAccountService.cs` and `ahis.template.identity/Services/AccountService.cs`. The closest mutation is `RevokeAdminUserSessionsCommand.cs` and `AccountService.RevokeAdminUserSessionsAsync`: the controller dispatches the route target and proof, the handler obtains the actor from `ICurrentUserService`, the service validates the actor proof before target lookup, then uses an `IdentityUnitOfWork` transaction and `IIdentityTokenStateService.InvalidateAsync`. The handler attempts a target-scoped audit after commit.

`AccountSecurityProofService.cs` issues five-minute proofs bound to the user ID and current security version and validates with zero clock skew. `IdentityTokenStateService.cs` rotates the security stamp and revokes active refresh tokens and sessions. Bearer validation in `ahis.template.api/Program.cs` requires a valid security version and active session. `ApplicationUser.cs` defines `IsLockedOut` from `LockoutEnabled` and a future `LockoutEnd`; it also has `IsActive` and `IsDeleted`. `Program.cs` configures Identity's lockout duration and failed-attempt limit. The observed login path uses Identity password sign-in with lockout on failure.

## Proposed request flow and business behavior

1. Add `POST {userId}/unlock` to `AdminIdentityController` with a dedicated bearer-authenticated `IdentityAdminUserUnlockPolicy` requiring `IdentityRoleNames.Superadmin`. Apply the existing `AuthenticatedSecurityPolicy` limiter. API-client credentials and permissions cannot satisfy the policy. Global target scope applies to authorized Superadmins; add no tenant or support boundary.
2. Bind the optional `X-Step-Up-Proof` header and route `userId`; accept no request body. Dispatch a new `UnlockAdminUserCommand` through `IMediator` with `HttpContext.RequestAborted`. The handler derives the actor ID solely from `ICurrentUserService` and rejects missing actor, blank route ID, missing proof, and self-targeting before mutation. Self-unlock is prohibited even if the target would otherwise be eligible.
3. In the Identity service, validate the existing five-minute proof against the **actor**, before looking up the target. A missing, expired, stale, or wrong-actor proof gets the same generic `400` response. Only after authorization and valid proof may an unknown target produce `404`.
4. Evaluate target state within the Identity transaction using a fresh target read and a single captured UTC time. Refuse inactive or soft-deleted targets, disabled lockout, expired or otherwise not-currently-locked states, and permanent or administrative holds. An active, non-deleted target with `LockoutEnabled = true` and `LockoutEnd = null` is the clean already-unlocked case: return idempotent `204` without writing user/token state, notification, or unlock audit. A non-null `LockoutEnd` at or before the captured time is expired and must be refused, not silently treated as that clean case.
5. For a proven active ordinary lockout, use Identity manager operations to set `LockoutEnd` to null and `AccessFailedCount` to zero, keeping `LockoutEnabled` true. Invoke `IIdentityTokenStateService.InvalidateAsync(target)` in the same `IdentityContext` transaction, then commit. A failure in any Identity operation or token/session update rolls back the entire unlock. Use concurrency-aware updates; a racing state change must be re-evaluated or fail safely, never leave a partial unlock with live sessions.
6. After a confirmed commit, attempt one target notification through the existing email sender pattern and one explicit audit event. Notification or audit failure, including request cancellation after commit, must not turn the committed unlock into an HTTP failure. Do not notify or audit an idempotent already-unlocked result. Return `204` with `Cache-Control: no-store` and no response body.

**Lockout classification evidence and constraint:** Current source has `LockoutEnabled`, `LockoutEnd`, and `AccessFailedCount`, but no administrative-hold type, provenance, or writer. Implement the ordinary-lockout check conservatively against the configured normal Identity lockout window; reject sentinel or out-of-window future end values as holds. If actual deployment data can contain a hold indistinguishable from an ordinary lockout inside that window, these fields cannot prove eligibility. Resolve that storage/provenance precondition under a separately reviewed change before enabling unlock for such data; do not silently unlock an ambiguous hold or add a schema change under this blueprint.

## Authorization, security, and API contract

- `401 Unauthorized`: missing or invalid bearer token. The dedicated policy explicitly selects the JWT bearer scheme.
- `403 Forbidden`: authenticated principal lacks the dedicated Superadmin policy, including API-client principals.
- Generic `400 Bad Request`: missing or invalid proof, blank route ID, self-targeting, or an existing ineligible target. Do not disclose lockout category or target security state in the response.
- `404 Not Found`: unknown target, reachable only after authorization and valid actor proof; use a generic response without echoing the target ID.
- `204 No Content`: committed unlock or clean already-unlocked idempotent request. No target/security response body and no security-state projection.
- `429 Too Many Requests`: existing five-per-minute, per-actor `AuthenticatedSecurityPolicy` limiter. Document a generic operational `500` for a failure before a confirmed commit.

Set `Cache-Control: no-store` on action responses. Add accurate Swagger summary, proof-header remarks, and status metadata. Do not log or return the proof, bearer, refresh cookie, security stamp, token hashes, credentials, email address, or unmasked audit values. A target unlock does not clear the actor's refresh cookie because self-unlock is forbidden.

## Files to add or modify after approval

- `ahis.template.api/Controllers/v1/AdminIdentityController.cs`: new action, policy and limiter attributes, proof binding, result/status mapping, no-store header, and Swagger metadata.
- `ahis.template.api/Program.cs`: register dedicated bearer-only `IdentityAdminUserUnlockPolicy` using `IdentityRoleNames.Superadmin`; reuse the existing limiter without changing its five-per-minute configuration.
- `ahis.template.application/Features/AccountFeatures/Commands/UnlockAdminUserCommand.cs` (new): mediator request/handler, actor provenance, generic validation and target-not-found mapping, and audit only for a committed unlock.
- `ahis.template.identity/Interfaces/IAccountService.cs` and `ahis.template.identity/Services/AccountService.cs`: narrow proof validation, self-target rejection, target classification, transactional Identity unlock and invalidation, and post-commit best-effort notification. Return a narrow outcome that distinguishes missing, ineligible, already unlocked, and committed unlock without exposing target security state to the API.
- `ahis.template.test/TestFeatures/AccountFeature/UnlockAdminUserCommandHandlerTest.cs` (new) and `ahis.template.test/TestFeatures/AccountFeature/UnlockAdminUserAccountServiceTest.cs` (new): focused handler and service behavior. Add an authenticated API/relational test fixture in `ahis.template.test` if needed to verify policy attachment, transaction rollback, and persistence semantics.
- After implementation is verified, update `docs/backlog/identity-security-endpoints.md`, `docs/modules/account.md`, and `docs/architecture/authentication-authorization.md` to describe the shipped contract and evidence. Update action XML/Swagger remarks with the code.

Intentionally leave `ApplicationUser`, `IdentityContext`, migrations, production settings, role bootstrap, API-client permissions, other administrative endpoints, password/MFA state, and unrelated documentation unchanged. If hold provenance requires schema work, stop and obtain a separate approved blueprint rather than expanding this change.

## Persistence, transaction, audit, and integration impact

Use the existing `IdentityContext` user, refresh-token, and refresh-session tables. `IdentityUnitOfWork` owns the Identity transaction. The lockout-field updates, security-stamp rotation, and refresh-token/session revocations must commit or roll back together. A successful invalidation makes target access tokens and actor-bound target proofs fail their existing security-version checks and makes refresh sessions unusable. Do not rotate token/security state for the clean already-unlocked case. No endpoint-specific migration, new dependency, new secret, or production configuration change is planned.

For an actual committed unlock, write one explicit `IAuditLogger.LogAsync` event using `AuditActionEnum.StatusChange`, entity `AccountSecurity`, target ID as `entityId`, and a fixed label such as `AdminUserUnlocked`. `AuditLogger` derives the actor from `ICurrentUserService` and writes to `ApplicationDbContext`, separately from the Identity transaction; audit delivery is therefore best effort. Catch post-commit audit exceptions without changing success. Attempt target notification only after commit using the existing `NotifySecurityChangeAsync`/email-sender pattern; suppress and safely log delivery failure. Neither external effect is part of the Identity transaction.

## Test plan and validation after approval

- Handler tests: actor comes from the authenticated principal, target from the route; reject blank IDs, missing proof, and self-targeting; pass the actor-bound proof; map unknown target to generic `404`; audit exactly once for a committed unlock with actor/target context and fixed metadata; no audit for ineligible or already-unlocked results; audit failure preserves committed `204`.
- Identity service tests: invalid, expired, stale, and wrong-actor proofs fail before target lookup; active ordinary lockout clears only the approved fields and invalidates token/session/proof state; disabled lockout, inactive, soft-deleted, expired, not-currently-locked, permanent/hold, and self-target cases do not mutate; clean already-unlocked returns `204` without stamp rotation, audit, or notification; notification failure after commit preserves success. Cover boundary times and a concurrent target change.
- Relational integration tests: lockout-field update and `InvalidateAsync` token/session changes are atomic; a forced pre-commit failure rolls all changes back; a committed unlock rejects previously valid target bearer, refresh session, and step-up proof. Mock-only tests cannot establish these transaction properties.
- API integration tests: missing/invalid bearer `401`; non-Superadmin or API key denied; invalid proof gives generic `400` without target lookup; missing target gives `404` only with valid authorization/proof; success and idempotent cases return empty `204` with no-store; limiter returns `429`; Swagger and response/audit redaction match the contract.

Run focused tests first, then concise solution checks **after implementation approval**:

```powershell
dotnet test .\ahis.template.test\ahis.template.test.csproj --filter "FullyQualifiedName~UnlockAdminUser" --verbosity quiet
dotnet restore .\AhisApiTemplate.sln --verbosity quiet
dotnet build .\AhisApiTemplate.sln --no-restore --verbosity quiet
dotnet test .\AhisApiTemplate.sln --no-build --no-restore --verbosity quiet
```

No build, test, migration generation/application, database update, deployment, or external write is part of this planning pass.

## Risks, assumptions, and remaining infrastructure dependencies

- **Role provisioning and store alignment:** `Program.cs` configures `AddRoles<IdentityRole>()`, while `IdentityContext.cs` explicitly maps `IdentityRole<string>` to `IdentityRoles`; `docs/blueprints/2026-09-25-identity-role-store-alignment.md` records the mismatch. A human Superadmin must be provisioned in the authoritative role store and receive a fresh bearer with that role before the policy is usable. This endpoint does not repair or seed roles.
- **Hold provenance:** Source has no explicit administrative-hold marker. The conservative normal-window classifier must be checked against real permitted data semantics before implementation; any indistinguishable hold requires separate provenance work. The required behavior remains refusal of all holds.
- **Commit boundary:** Existing service patterns place post-commit work near broad exception handling. The new path must record when commit is confirmed so cancellation, notification, or audit failure cannot misreport a committed unlock as failure.

Assume Identity user IDs remain opaque strings and the existing proof, limiter, notification sender, audit logger, and token-state service are reused. Global Superadmin target scope, self-unlock prohibition, eligible-target rules, idempotency, and HTTP contract are owner-resolved; they are not open questions.