# P1 blueprint: Revoke every session

Status: Proposed for approval (2026-09-22). Planning only; no implementation or migration is authorized by this document.

## Outcome

Add `POST /api/account/sessions/revoke-all` for an authenticated account owner who presents a valid five-minute step-up proof. A successful request revokes the owner's existing refresh sessions, rotates the Identity security stamp so existing access tokens and step-up proofs fail validation, clears the caller's refresh cookie, and returns `204 No Content`. The current session is included. A later, fresh login remains possible.

## Owning module and analogous implementation

Account owns the self-service endpoint. The nearest path is `AccountController` -> custom `IMediator` -> Account command handler -> `IAccountService` / `AccountService`, as used by `ResetAuthenticator` and `Deactivate`. `AccountService` already checks step-up proofs, uses `IdentityUnitOfWork` for security changes, and invokes `IIdentityTokenStateService.InvalidateAsync`. The latter updates the security stamp and revokes user refresh-token and session rows. `AuthenticationService.RevokeRefreshTokensAsync` is an internal analogue, but it does not validate a step-up proof or expose a self-service route.

## Observed flow and proposed flow

Observed: `re-authenticate` issues a signed proof bound to the user and current security version. Bearer validation requires an access-token-use claim, current security version, and eligible account. Refresh rotation checks account state and stored security version. Other Account security actions validate the proof, invalidate token state within an Identity transaction, then clear the refresh cookie in the controller.

Proposed:

1. `[Authorize]` authenticates an access bearer; the per-user `AuthenticatedSecurityPolicy` rate limit applies.
2. The controller accepts only `{ "stepUpProof": "..." }`, dispatches a custom mediator command, and passes `HttpContext.RequestAborted` where available. No user ID or session ID is accepted from the request.
3. The handler obtains the user ID from `ICurrentUserService`, calls `IAccountService.RevokeAllSessionsAsync`, and attempts a secret-free audit event for success and invalid proof.
4. The Identity service looks up the user, verifies eligibility and the proof through `IAccountSecurityProofService`, and opens an `IdentityUnitOfWork` transaction. It calls `IIdentityTokenStateService.InvalidateAsync` and commits only after the security-stamp update and both bulk revocations succeed. Any failure rolls back; the response is never success for a partial change.
5. After commit, the service attempts an account security notification. The controller clears the canonical and legacy refresh-cookie paths with `RefreshCookie.Clear` and returns `204`. Clients discard the old access token and proof and sign in again.

The stamp change is the immediate enforcement point for access tokens and proof validity. The same Identity transaction groups the stamp update and stored token/session revocations. A login that begins after the operation may create a new session; the endpoint does not disable the account.

## Files to add / modify / intentionally leave unchanged

| File | Planned change |
| --- | --- |
| `ahis.template.api/Controllers/v1/AccountController.cs` | Add the authenticated, rate-limited route, status documentation, success-only cookie clear, and generic failure mapping. |
| `ahis.template.application/Features/AccountFeatures/Commands/AccountSecurityCommands.cs` | Add `RevokeAllSessionsCommand` and handler using the current principal and audit logger. |
| `ahis.template.identity/Interfaces/IAccountService.cs` | Add `RevokeAllSessionsAsync(userId, stepUpProof, cancellationToken)`. |
| `ahis.template.identity/Services/AccountService.cs` | Validate proof and eligibility, transact through existing token-state invalidation, and notify after commit. |
| `ahis.template.identity/Services/AuthenticationService.cs` | In refresh rotation, require the conditional active-session update to affect one row before saving a replacement token; fail and roll back if revocation won the race. Preserve unrelated edits already in this file. |
| `ahis.template.test/TestFeatures/AccountFeature/...` and `ahis.template.test/TestFeatures/AuthenticationFeature/...` | Add focused command, proof/Identity, and refresh-race coverage as described below. |
| `docs/backlog/identity-security-endpoints.md`, `docs/modules/account.md`, `docs/modules/authentication.md`, `docs/architecture/authentication-authorization.md` | After implementation, record completion evidence and the user-visible session semantics. |

Keep `RefreshCookie`, `IIdentityTokenStateService`, `IdentityContext`, entity mappings, migrations, `Program.cs` rate-limit policy, and the existing internal `RevokeRefreshTokensAsync` contract unchanged unless implementation evidence reveals a gap. No new repository, domain entity, or dependency is planned.

## Business behavior and confidence

The backlog explicitly recommends revoking the current session; this blueprint adopts that default. The operation is all-or-fail at the Identity database transaction level. Repeating it requires a newly authenticated bearer and a new step-up proof because the first success invalidates both. A valid request succeeds even when no refresh-session rows remain; stamp rotation still invalidates outstanding access tokens. The existing step-up proof is reusable only until expiry or stamp rotation, and a successful revocation invalidates it.

These behaviors are proposed decisions, not pre-existing endpoint guarantees. Existing source confirms the needed token-version and session primitives.

## Authorization and security

Use `[Authorize]` with the default bearer scheme and `AuthenticatedSecurityPolicy` (currently five requests per user per minute). No administrative policy or client-supplied user selector is needed. Invalid, expired, wrong-user, or stale-version proofs yield the same generic validation response. Missing/invalid bearer yields `401` before the handler. No password, proof, access/refresh token, token hash, or cookie value may enter logs, audit metadata, or response bodies. Audit actor and entity ID come from the authenticated principal.

The action needs no new password lockout operation because it consumes an already issued proof; failures still count toward the existing per-user rate limit. The current bearer is valid for this request, then fails on subsequent requests after commit. Cookie clearing is browser cleanup; server-side invalidation is authoritative.

## API contract

- `POST /api/account/sessions/revoke-all`
- Request JSON: `{ "stepUpProof": "<proof from /api/account/re-authenticate>" }`; required, no user ID.
- Success: `204 No Content`, with expired `refresh_token` cookies at the canonical and transitional paths.
- `400 ValidationProblemDetails`: missing or invalid proof, with a generic message.
- `401`: unauthenticated/invalid bearer; `429`: existing authenticated security limit; `500 ProblemDetails`: transaction or other unexpected server failure, with no internal detail.

Do not clear the cookie or claim revocation on an operation that did not commit. Use the Account controller's existing response style; give the handler/controller an internal error category for invalid proof versus operational failure so database errors are not presented as invalid user input. Do not expose the internal category to clients. Add XML/Swagger remarks explaining that all sessions, including the caller's, end and a fresh login is required.

## Entity, persistence, and migration impact

No schema or migration change is planned. `IdentityContext` already has `RefreshTokens`, `RefreshSessions`, and the user's security stamp. `IIdentityTokenStateService.InvalidateAsync` changes them through one context; the new Account service must start and commit its transaction around that call. Use `CancellationToken.None` for rollback cleanup. Existing retention and cleanup rules continue to apply to revoked rows.

For an in-flight refresh, the proposed one-row active-session update check prevents a replacement token from being saved if session revocation has already won. Concurrent transactions may also conflict or deadlock; those cases must roll back and return a generic failure. Security-version validation makes an old-version token unusable even if its creation raced the revocation. A genuinely new login after revocation is allowed.

## Service decision and evidence

Keep orchestration in `IAccountService` / `AccountService` because proof validation, Identity user lookup, security-stamp rotation, refresh persistence, transaction handling, and notification are a single Identity workflow. The handler remains thin. Call the shared `IIdentityTokenStateService` rather than duplicating its bulk updates or adding an Application repository. No DI registration is needed for a new service.

## Transaction, audit, and integration impact

The Identity stamp and bulk token/session changes commit or roll back together. After commit, attempt a `Logout` audit event with `AccountSecurity` and metadata such as `AllSessionsRevoked`; record invalid-proof attempts with a neutral account-security failure label. The existing `IAuditLogger` writes through `ApplicationDbContext`, so this audit is best effort and cannot be claimed as part of the Identity transaction. Audit failures must not undo successful revocation. Send a security notice to the account email after commit through the existing best-effort notification helper; delivery failure is logged without secrets and does not restore sessions. No new external integration or configuration is planned.

## Test plan

- Handler/controller: unauthorized requests never dispatch; no request user ID can override the principal; invalid/missing proof is generic; success clears the cookie and returns `204`; failed transaction does not clear it; `429` uses the selected policy; audit metadata excludes secrets.
- Identity behavior: valid proof revokes every owned token/session and changes the security version; zero existing sessions still succeeds; wrong-user, expired, malformed, and stale-version proofs leave state unchanged; a failed stamp update or bulk update rolls the transaction back.
- End-to-end security: old bearer and step-up proof fail after success; old refresh tokens fail; another user's sessions remain active; new login can create a usable session.
- Concurrency: race refresh rotation with revoke-all and verify that a replacement token is never usable after revocation, and that a session already revoked cannot accept a new replacement. Exercise the transaction behavior with a relational Identity test fixture, preferably SQL Server for its actual isolation and locking behavior. Do not use an EF mock as proof of atomicity.

The current test project has proof and token-state unit tests but no committed API/transaction fixture. Add focused tests without changing production dependencies; a database-backed concurrency test needs an isolated test database and separate authorization before it mutates that database. If unavailable, report that concurrency remains unverified rather than claiming the unit tests prove it.

## Documentation impact

After implementation, check the six revoke-all action items in the backlog, add completion date and links to the endpoint, service, tests, and this blueprint, and update the progress row. Document the request/response, cookie path, current-session effect, stamp-based access-token invalidation, audit/notification behavior, and limits of any concurrency verification in module/security docs. No migration documentation update is needed.

## Validation commands

From the repository root, run focused affected-project builds and new-test filters first, restoring only if needed; then run `dotnet build .\AhisApiTemplate.sln --no-restore` and `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`. Record exact results. Do not apply an EF migration or run a database update as routine verification.

## Risks, assumptions, and open questions

- The plan adopts the backlog's default to revoke the current session and assumes normal fresh login remains permitted.
- Proof validation occurs before the transaction, while the stamp update occurs inside it. A concurrent security change should fail closed through the version check/Identity concurrency behavior; verify this in relational testing.
- Existing explicit audit writes are best effort and cross a separate context. A transactional security audit would be a separate architecture change.
- The local worktree already has unrelated edits in `AuthenticationService`, `AuthenticationController`, `Program.cs`, and docs; implementation must preserve them.
- Approval is required for the new public route and its security behavior. If implementation uncovers a required schema, dependency, or broader auth-flow change, revise this blueprint before proceeding.

## Runtime failure amendment — approved (2026-09-22)

The implemented endpoint returned a generic `500` after `AccountService.RevokeAllSessionsAsync` called `IdentityTokenStateService.InvalidateAsync`. The reported `MissingMethodException` names the EF Core 8 `ExecuteUpdateAsync` method signature. The Identity project directly references EF Core SQL Server and Tools `8.0.22`, while API and Infrastructure directly reference EF Core `9.0.7`; the API resolves EF Core Relational `9.0.7` at runtime. Thus Identity's compiled bulk-update calls cannot bind to the loaded method. The same call pattern also exists in `AuthenticationService` refresh/session paths, so this is not isolated to revoke-all. The controller's generic `500` is the intended concealment of the internal exception, not the cause.

Approved repair: align the application's EF Core packages on `8.0.22`, matching its `net8.0` target and ASP.NET Core Identity EF Core `8.0.22` package. Change the direct EF Core/Design references in `ahis.template.api/ahis.template.api.csproj` and EF Core/SQL Server/Tools references in `ahis.template.infrastructure/ahis.template.infrastructure.csproj`. Leave Identity's EF Core references, code, database schema, migrations, endpoint contract, and security behavior unchanged.

Validation: restore, verify the API's resolved EF Core runtime graph is entirely `8.0.22`, build the affected projects and solution, run focused Identity/Account tests then the full test suite, and check the loaded EF Core method signature without connecting to or mutating a database. A database-backed endpoint check remains a separate environment-dependent validation; no migration will be applied. A broad EF Core downgrade may expose source or migration-compatibility errors during build, which must be assessed before any further scope change.
