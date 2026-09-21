# Identity Security Endpoint Backlog

## Purpose

This document tracks proposed security work for user identity, authentication, sessions, account recovery, and Identity administration. It is intended to make unimplemented work visible and to preserve the source evidence behind each proposal.

This is a backlog, not an approved implementation blueprint. Any item that changes a public API, authorization, security behavior, Identity persistence, schema, dependencies, or production configuration requires an approved change blueprint before implementation.

Last reviewed against source: 2026-09-22.

## Ownership boundary

- HTTP endpoints belong in `ahis.template.api`.
- Endpoint requests and handlers use the repository's custom mediator in `ahis.template.application`.
- ASP.NET Core Identity operations, refresh-token persistence, authentication coordination, and account-security workflows remain behind services in `ahis.template.identity`.
- Identity security endpoints must not introduce an application repository merely to wrap `UserManager`, `SignInManager`, or `IdentityContext`.

In this document, "Identity endpoint" describes the security capability and owner. It does not mean placing controllers inside the Identity class library.

## Status legend

- `[ ]` Not implemented or not confirmed in source
- `[x]` Confirmed in source
- `Blocked` Requires another backlog item or an external capability
- `Optional` Useful for some deployments but not part of the recommended first increment

When completing an item, add the completion date and links to the controller, service, tests, migration when applicable, and approved blueprint.

## Confirmed baseline

The following capabilities already have endpoints:

- [x] Registration
- [x] Email confirmation and confirmation-email resend
- [x] Initial password setup
- [x] Password change, forgot-password, and password reset
- [x] Password login and refresh-token rotation
- [x] Current-session logout
- [x] Authenticator setup, enable, disable, and login verification
- [x] Recovery-code consumption through the existing two-factor verification flow
- [x] Current-account details
- [x] Authentication rate limiting on selected endpoints
- [x] Internal refresh-token revocation for all user tokens through `RevokeRefreshTokensAsync`

Relevant source:

- [Account controller](../../ahis.template.api/Controllers/v1/AccountController.cs)
- [Authentication controller](../../ahis.template.api/Controllers/v1/AuthenticationController.cs)
- [Account Identity contract](../../ahis.template.identity/Interfaces/IAccountService.cs)
- [Authentication Identity contract](../../ahis.template.identity/Interfaces/IAuthenticationService.cs)
- [Authentication and authorization observations](../architecture/authentication-authorization.md)

## P0 — Security prerequisites

These should be addressed before exposing broader session-management or recovery capabilities.

### Token and account-state enforcement

- [x] Reject JWTs when the user is inactive or soft-deleted.
- [x] Include a SecurityStamp-derived version in issued access tokens and validate it on authenticated requests.
- [x] Make existing password change, password reset, MFA disable, and internal revoke-all/replay handling invalidate existing access tokens and refresh tokens.
- [x] Require the same invalidation in MFA-reset, email-change, and account-deactivation workflows.
- [x] Apply the same active, deleted, lockout, and security-version checks during refresh-token rotation.
- [x] Adopt immediate access-token revocation through per-request version validation.

Implementation evidence (2026-09-21): bearer validation checks `IsActive`, `IsDeleted`, lockout, an access-token-use claim, and a SecurityStamp-derived version. Refresh rotation applies the same account-state and version checks. MFA reset, confirmed email change, and account deactivation invoke `IIdentityTokenStateService.InvalidateAsync` after their Identity changes commit. `RefreshTokens.SecurityVersion` was added by `AddRefreshTokenSecurityVersion`; existing refresh rows without a version fail closed. Migration application is user-reported and was not independently performed by Codex.

### Refresh-token storage and session model

- [x] Store only a cryptographic hash of each refresh token. (2026-09-21)
- [x] Introduce a non-sequential, opaque public session identifier. (2026-09-21)
- [x] Record token family or parent/replacement relationships for replay detection. (2026-09-21)
- [x] Record session creation, last-used, expiry, and revocation information. (2026-09-21)
- [x] Decide whether to store bounded device name, client type, IP-derived information, and user-agent information. Device and network metadata are deferred pending a product privacy and retention policy. (2026-09-21)
- [x] Define retention and cleanup for expired and revoked sessions. Hashed token rows are retained for at least seven days after expiry; parent rows remain until their replacement history is removed. Ended sessions are retained for at least 30 days after expiry or revocation and are deleted only after their token rows. A hosted cleanup service removes at most 500 token rows and 500 session rows per daily run. (2026-09-21)
- [x] Add and review an `IdentityContext` migration; never apply it as routine verification. [`ReplaceRefreshTokensWithHashedSessionModel`](../../ahis.template.identity/Migrations/20260921074330_ReplaceRefreshTokensWithHashedSessionModel.cs) clears legacy raw-token rows before adding the required hash/session columns, so deployment requires users to sign in again. The migration was generated and source-reviewed; it was not applied. (2026-09-21)

Implementation evidence (2026-09-21): [`RefreshToken`](../../ahis.template.identity/Models/Entities/RefreshToken.cs) stores a SHA-256 `TokenHash`, session and parent-token keys, use timestamps, security version, and revocation state. [`RefreshSession`](../../ahis.template.identity/Models/Entities/RefreshSession.cs) supplies a random public `Guid` plus session lifecycle timestamps. [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) creates a session at login or completed 2FA, records rotation lineage, makes rotation conditional on an unrevoked token, and invalidates all user sessions on replay. [`IdentityTokenStateService`](../../ahis.template.identity/Services/IdentityTokenStateService.cs) revokes sessions during user-wide invalidation.

### Existing endpoint hardening

- [x] Replace public initial-password setup by user ID with a one-time, expiring, purpose-bound setup token. (2026-09-21)
- [x] Prevent authenticator-key replacement when MFA is already enabled, and fail safely when Identity key reset, key retrieval, or setup persistence fails.
- [x] Remove token encode/decode diagnostic endpoints from production. (2026-09-21)
- [x] Bind two-factor login completion to an opaque, short-lived pre-authentication challenge instead of trusting a freely supplied user ID. (2026-09-21)
- [x] Make forgot-password, resend-confirmation, and account-state responses consistently resistant to user enumeration. (2026-09-21)
- [x] Standardize refresh-cookie name, path, `Secure`, `HttpOnly`, and `SameSite` behavior across login, 2FA, refresh, and logout. (2026-09-21)
- [x] Ensure every public authentication and recovery endpoint has an explicit rate-limit decision. (2026-09-21)
- [x] Stop separately serializing reusable recovery-code material into `ApplicationUser`; use the Identity token store as the source of truth. (2026-09-21)

Implementation evidence (2026-09-21): [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) creates and validates a 30-minute `InitialPasswordSetup` Identity token and uses generic recovery responses. [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) issues a five-minute protected two-factor challenge, validates it before completing MFA, and clears it after use or failure. [`RefreshCookie`](../../ahis.template.api/Security/RefreshCookie.cs) centralizes the canonical `refresh_token` cookie settings and cleanup. [`Program.cs`](../../ahis.template.api/Program.cs) configures partitioned anonymous, authenticated, and refresh rate-limit policies. [`RemoveApplicationUserRecoveryCodes`](../../ahis.template.identity/Migrations/20260921082113_RemoveApplicationUserRecoveryCodes.cs) removes the duplicate recovery-code column; it was generated and source-reviewed but has not been applied.

## P1 — Recommended self-service endpoints

### Step-up authentication

- [x] `POST /api/account/re-authenticate`
  - [x] Require the current password.
  - [x] Require an authenticator code when MFA is enabled.
  - [x] Return a five-minute `step-up` proof bound to the user and current security version.
  - [x] Require the proof for MFA reset, email-change request, and deactivation.
  - [x] Rate-limit requests and audit successful and failed attempts without logging credentials or codes.

This is a dependency for session revocation, MFA reset, email change, and account deactivation.

### Revoke every session

- [x] `POST /api/account/sessions/revoke-all` (2026-09-22)
  - [x] Derive the user ID only from the authenticated principal.
  - [x] Require a valid step-up proof.
  - [x] Revoke all refresh-token sessions atomically.
  - [x] Invalidate existing access tokens according to the approved token-version policy.
  - [x] Revoke the current session.
  - [x] Clear the current refresh cookie on success.

Implementation evidence (2026-09-22): [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes the authenticated, rate-limited endpoint. [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) validates the proof and uses `IIdentityTokenStateService.InvalidateAsync` inside an Identity transaction. [`RevokeAllSessionsCommandHandlerTest`](../../ahis.template.test/TestFeatures/AccountFeature/RevokeAllSessionsCommandHandlerTest.cs) covers current-principal routing and success auditing. The API and Infrastructure EF Core references were aligned to `8.0.22`, matching Identity, after a production `MissingMethodException` from mixed EF Core 8/9 `ExecuteUpdateAsync` signatures. Focused tests (2) and the full suite (16) passed; relational concurrency coverage remains unimplemented.

### View active sessions

- [x] `GET /api/account/sessions` (2026-09-22)
  - [x] Return opaque session ID, creation time, last-used time, expiry, and current-session indicator.
  - [x] Never return raw or hashed refresh-token values.
  - [x] Return only sessions owned by the authenticated user.
  - [ ] Bounded device, IP, and location metadata remain deferred pending a product privacy and retention policy.

Implementation evidence (2026-09-22): [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes the authenticated, rate-limited paginated endpoint. [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) projects active owner sessions from `IdentityContext` without token values. New access tokens carry the existing opaque session public ID so the current row can be identified; older access tokens have no current-session indicator until refreshed or replaced by a new login. [`GetActiveSessionsQueryHandlerTest`](../../ahis.template.test/TestFeatures/AccountFeature/GetActiveSessionsQueryHandlerTest.cs) covers principal routing, pagination bounds, response projection, and audit routing.

### Revoke one session

- [x] `DELETE /api/account/sessions/{sessionId}` (2026-09-22)
  - [x] Verify ownership of the opaque session ID.
  - [x] Require a recent step-up proof when revoking a session other than the current one.
  - [x] Make repeated revocation idempotent.
  - [x] Clear the refresh cookie when the current session is selected.
  - [x] Audit the action without logging tokens.

Implementation evidence (2026-09-22): [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) accepts the optional `X-Step-Up-Proof` header and clears the current refresh cookie only for the selected current session. [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) revokes owner sessions and their active refresh tokens atomically; unknown, ended, and other-account IDs are an indistinguishable successful response. Bearer validation now requires an active opaque session ID, so revocation immediately invalidates that session's access token. A revoked token from an ended session no longer triggers account-wide replay invalidation.

### Security summary

- [x] `GET /api/account/security-summary` (2026-09-22)
  - [x] Return email-confirmed, phone-confirmed, password-present, MFA-enabled, authenticator-configured, remaining-recovery-code count, and active-session count.
  - [x] Never return authenticator keys, provisioning URIs, recovery codes, tokens, or password data.
  - [x] Do not return last password change or last security-sensitive event because reliable timestamps are not available.

Implementation evidence (2026-09-22): [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) uses ASP.NET Core Identity for credential state and recovery-code count, and the active-session predicate used by session listing. [`GetSecuritySummaryQueryHandlerTest`](../../ahis.template.test/TestFeatures/AccountFeature/GetSecuritySummaryQueryHandlerTest.cs) covers current-principal routing, response projection, and audit routing.

### Regenerate recovery codes

- [ ] `POST /api/account/2fa/recovery-codes/regenerate`
  - [ ] Require MFA to be enabled and require recent step-up authentication.
  - [ ] Replace all previous recovery codes.
  - [ ] Return new codes exactly once.
  - [ ] Never log or persist a second plaintext copy of the codes.
  - [ ] Notify the user that recovery codes changed.

### Reset authenticator

- [x] `POST /api/account/2fa/reset-authenticator`
  - [x] Require a recent step-up proof.
  - [x] Disable the existing authenticator and invalidate old recovery codes.
  - [x] Revoke refresh sessions and invalidate access tokens.
  - [x] Require the normal authenticator setup and verification flow before re-enabling MFA.
  - [x] Send a security notification to the account email.

### Verified email change

- [x] `POST /api/account/change-email/request`
  - [x] Require a recent step-up proof.
  - [x] Validate uniqueness without exposing whether another account owns the address.
  - [x] Generate an ASP.NET Core Identity change-email token.
  - [x] Send confirmation to the new address and a security notification to the old address.
  - [x] Do not change the current email until confirmation succeeds.
- [x] `POST /api/account/change-email/confirm`
  - [x] Consume a one-time, expiring, purpose-bound token.
  - [x] Update the normalized email through `UserManager`.
  - [x] Update username when it matched the previous email.
  - [x] Revoke refresh sessions and invalidate access tokens.
  - [x] Return a generic error for invalid, expired, or already-used tokens.

### Account deactivation

- [x] `POST /api/account/deactivate`
  - [x] Require a recent step-up proof and explicit confirmation.
  - [x] Set the existing inactive and soft-delete state.
  - [x] Revoke all refresh sessions and invalidate access tokens.
  - [x] Prevent new login and refresh.
  - [x] Send a security notification and record a security audit event.
  - [x] Deactivation has no self-service reversal path; recovery requires support intervention.

## P2 — Account recovery

Recovery is high risk and requires an owner-approved policy covering identity proofing, abuse handling, notifications, and support escalation.

- [ ] `POST /api/account/recovery/start`
  - [ ] Return the same response whether or not the account exists.
  - [ ] Apply strict per-IP and per-account rate limits.
  - [ ] Issue only a short-lived, single-use, purpose-bound recovery challenge.
  - [ ] Notify the user through already verified channels.
- [ ] `POST /api/account/recovery/complete`
  - [ ] Require the approved recovery evidence.
  - [ ] Prevent bypass of stronger MFA without an explicit recovery policy.
  - [ ] Reset the affected credential, revoke all sessions, and invalidate access tokens.
  - [ ] Notify the user of the completed recovery.
  - [ ] Record sufficient audit evidence without storing secrets.

## P2 — Administrative Identity controls

All administrative endpoints require a dedicated administrative authorization policy, actor/target audit records, and protection against an administrator modifying their own access in an unsafe way.

- [ ] `GET /api/admin/identity/users/{userId}/security-state`
- [ ] `POST /api/admin/identity/users/{userId}/revoke-sessions`
- [ ] `POST /api/admin/identity/users/{userId}/unlock`
- [ ] `POST /api/admin/identity/users/{userId}/disable`
- [ ] `POST /api/admin/identity/users/{userId}/enable`
- [ ] `POST /api/admin/identity/users/{userId}/reset-mfa`
- [ ] `POST /api/admin/identity/users/{userId}/require-password-reset`

Administrative responses must never contain password hashes, authenticator secrets, raw recovery codes, raw refresh tokens, reset tokens, signing keys, or unmasked sensitive audit values.

## P2 — User role management

ASP.NET Core Identity role storage and JWT role claims already exist, but no confirmed endpoints or Identity service contract manage roles or user-role assignments. The README also identifies role and permission management as a future improvement.

Role management should remain in the Identity boundary and use `RoleManager<IdentityRole>` and `UserManager<ApplicationUser>`. It must not reuse API-client permissions, which belong to the separate API-client authentication module and use a different principal and claim type.

### Role authorization model

- [ ] Define the initial role catalog and identify protected system roles.
- [ ] Decide whether roles alone are sufficient or whether human-user permissions will be represented by Identity role claims.
- [ ] Define dedicated policies for reading roles, managing roles, and assigning roles.
- [ ] Define a secure first-administrator bootstrap process that is unavailable after bootstrap.
- [ ] Prevent deletion or unsafe renaming of protected system roles.
- [ ] Prevent removal or deactivation of the last effective administrator.
- [ ] Define whether administrators may change their own roles; default recommendation is to prohibit self-escalation and last-administrator self-demotion.
- [ ] Define how a role or role-claim change invalidates access tokens for every affected user.
- [ ] Review the current Identity migration/model before implementation because source evidence shows both `AspNetRoles` and `IdentityRoles` mappings; confirm the authoritative role table and relationships before generating any migration.

Role and role-claim changes are blocked on the P0 token/security-version policy because current JWTs contain a snapshot of roles at issuance.

### Role catalog endpoints

- [ ] `GET /api/admin/identity/roles`
  - [ ] Require the role-read policy.
  - [ ] Support bounded pagination and optional normalized-name filtering.
  - [ ] Return role ID, name, protected status, and assignment count; do not expose internal concurrency values unless they are deliberately used as an ETag.
- [ ] `GET /api/admin/identity/roles/{roleId}`
  - [ ] Return role details and approved claims/permissions.
  - [ ] Return a consistent not-found response without exposing unrelated user data.
- [ ] `POST /api/admin/identity/roles`
  - [ ] Require the role-management policy and recent step-up authentication.
  - [ ] Normalize names through `RoleManager`; reject blank, duplicate, reserved, or invalid names.
  - [ ] Audit actor, created role, timestamp, and request correlation without sensitive values.
- [ ] `PUT /api/admin/identity/roles/{roleId}`
  - [ ] Require the role-management policy and recent step-up authentication.
  - [ ] Use optimistic concurrency for updates.
  - [ ] Protect system-role names and define the effect of renaming a role referenced by policies or configuration.
- [ ] `DELETE /api/admin/identity/roles/{roleId}`
  - [ ] Require the role-management policy and recent step-up authentication.
  - [ ] Reject protected roles and roles with assignments unless an explicit reassignment workflow is approved.
  - [ ] Make the expected behavior for repeated deletion explicit.

### User-role assignment endpoints

- [ ] `GET /api/admin/identity/users/{userId}/roles`
  - [ ] Require the role-read policy.
  - [ ] Return direct Identity role assignments only, unless inherited access is introduced later.
- [ ] `PUT /api/admin/identity/users/{userId}/roles`
  - [ ] Require the role-assignment policy and recent step-up authentication.
  - [ ] Treat the submitted role IDs as an atomic desired set to avoid partial assignment.
  - [ ] Validate all roles before making changes.
  - [ ] Prevent self-escalation, unsafe self-demotion, and removal of the last administrator.
  - [ ] Update the user's token/security version and revoke refresh sessions according to the approved policy.
  - [ ] Audit added and removed roles with actor and target IDs.
- [ ] `GET /api/admin/identity/roles/{roleId}/users`
  - [ ] Require the role-read policy.
  - [ ] Return a bounded, paginated projection rather than full Identity user records.

An atomic `PUT` is recommended for assignment because it gives the server one validated target state. Separate `POST .../roles/{roleId}` and `DELETE .../roles/{roleId}` operations may be used instead if the product requires granular changes, but the API should not expose both styles without a clear consistency rule.

### Optional human-user permission endpoints

Only add these if the approved authorization model uses permissions in addition to roles:

- [ ] `GET /api/admin/identity/roles/{roleId}/permissions`
- [ ] `PUT /api/admin/identity/roles/{roleId}/permissions`
- [ ] Define a canonical, allow-listed permission catalog rather than accepting arbitrary claim types or values.
- [ ] Emit human-user permissions under a claim type distinct from API-client `api_permission` claims unless a reviewed policy deliberately supports both principal types.
- [ ] Invalidate authorization state for all assigned users after a permission change.
- [ ] Add policy-level tests proving that roles and permissions are actually applied to protected actions.

Do not provide a general-purpose endpoint that lets administrators write arbitrary claims. Claim types and values should be constrained by a server-owned catalog.

## P3 — Optional passkey/WebAuthn support

This is a separate initiative because it requires credential persistence, dependency review, relying-party configuration, origin validation, migrations, browser ceremony handling, and recovery-policy decisions.

- [ ] `POST /api/account/passkeys/registration-options`
- [ ] `POST /api/account/passkeys/registration-complete`
- [ ] `GET /api/account/passkeys`
- [ ] `DELETE /api/account/passkeys/{credentialId}`
- [ ] `POST /api/authentication/passkeys/options`
- [ ] `POST /api/authentication/passkeys/complete`

## Cross-cutting definition of done

Apply this checklist to every selected endpoint:

- [ ] An approved blueprint identifies the owner, request path, public contract, security behavior, persistence impact, migration impact, and rollback considerations.
- [ ] The action has an explicit authentication and authorization decision.
- [ ] User identity is derived from trusted claims or a purpose-bound challenge, not an unrestricted client-supplied user ID.
- [ ] Sensitive actions use step-up authentication where required.
- [ ] Validation and error responses do not leak account existence or secret material.
- [ ] Rate limiting and lockout effects are explicitly defined.
- [ ] Cancellation is propagated where surrounding APIs support it.
- [ ] Security events are audited without passwords, codes, raw tokens, authenticator keys, or other secrets.
- [ ] User notifications are sent for material credential or security changes.
- [ ] Unit and/or integration tests cover success, unauthorized access, wrong-user access, replay, expiry, rate limiting, and revocation behavior.
- [ ] Swagger documentation accurately states cookies, authentication requirements, status codes, and one-time secret handling.
- [ ] Focused validation passes before solution-wide restore, build, and test.
- [ ] Relevant module and architecture documentation is updated.

## Progress record

Add one row when an item moves beyond backlog status.

| Item                                   | Status      | Blueprint           | Completed  | Evidence/notes                                                                                                                                                                                                                                                                                                                                                                                          |
| -------------------------------------- | ----------- | ------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| P0 token and account-state enforcement | Implemented | Approved 2026-09-21 | 2026-09-21 | `AddRefreshTokenSecurityVersion` generated and user-reported as applied; MFA reset, email change, and deactivation call [`InvalidateAsync`](../../ahis.template.identity/Services/IdentityTokenStateService.cs) after their Identity changes; step-up proof coverage is in [`AccountSecurityProofServiceTest`](../../ahis.template.test/TestFeatures/AccountFeature/AccountSecurityProofServiceTest.cs) |
| P0 refresh-token/session model         | Implemented | Approved 2026-09-21 | 2026-09-21 | Hashed rotation lineage, replay invalidation, random public session IDs, lifecycle data, and bounded daily cleanup are implemented. `ReplaceRefreshTokensWithHashedSessionModel` was generated and source-reviewed; it was not applied. |
| P0 existing endpoint hardening         | Implemented | Approved 2026-09-21 | 2026-09-21 | Password setup uses a 30-minute purpose-bound Identity token; diagnostics and account-state routes were removed; 2FA uses a five-minute protected challenge; refresh cookies are centralized; public authentication and recovery endpoints have partitioned limits; `RemoveApplicationUserRecoveryCodes` was generated and reviewed but not applied. |
| Step-up authentication                 | Implemented | Approved 2026-09-21 | 2026-09-21 | Five-minute proof bound to the user and current security version                                                                                                                                                                                                                                                                                                                                        |
| Revoke every session                   | Implemented | Approved 2026-09-22 | 2026-09-22 | [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) revokes the authenticated user's current and other sessions with a step-up proof; the Identity security version invalidates access tokens and proofs. EF Core references were aligned to `8.0.22` after a mixed-version runtime failure. Focused tests (2) and the full suite (16) passed; relational concurrency verification remains pending. |
| View active sessions                   | Implemented (metadata deferred) | Approved 2026-09-22 | 2026-09-22 | Authenticated, paginated owner-only listing of active sessions; access-token session claim identifies the current row. Device, IP, and location collection remain deferred pending privacy and retention policy. |
| Revoke one session                     | Blocked     | —                   | —          | Requires active-session support                                                                                                                                                                                                                                                                                                                                                                         |
| Security summary                       | Backlog     | —                   | —          | —                                                                                                                                                                                                                                                                                                                                                                                                       |
| Regenerate recovery codes              | Backlog     | —                   | —          | —                                                                                                                                                                                                                                                                                                                                                                                                       |
| Reset authenticator                    | Implemented | Approved 2026-09-21 | 2026-09-21 | Requires step-up proof and invalidates credentials                                                                                                                                                                                                                                                                                                                                                      |
| Verified email change                  | Implemented | Approved 2026-09-21 | 2026-09-21 | Uses Identity change-email token and invalidates credentials at confirmation                                                                                                                                                                                                                                                                                                                            |
| Account deactivation                   | Implemented | Approved 2026-09-21 | 2026-09-21 | Inactive and soft-delete state with no self-service reversal                                                                                                                                                                                                                                                                                                                                            |
| Account recovery                       | Backlog     | —                   | —          | Requires approved recovery policy                                                                                                                                                                                                                                                                                                                                                                       |
| Administrative Identity controls       | Backlog     | —                   | —          | Requires dedicated admin policy                                                                                                                                                                                                                                                                                                                                                                         |
| User role management                   | Blocked     | —                   | —          | Requires role model decisions and P0 token invalidation                                                                                                                                                                                                                                                                                                                                                 |
| Passkey/WebAuthn support               | Optional    | —                   | —          | Separate initiative                                                                                                                                                                                                                                                                                                                                                                                     |

## Source observations behind this backlog

- [`Program.cs`](../../ahis.template.api/Program.cs) validates JWT issuer, audience, lifetime, signing key, user existence, active/deleted state, lockout, and a SecurityStamp-derived version.
- [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) rotates hash-addressed refresh tokens, preserves parent relationships, updates session activity, and invalidates user token state on replay; refresh validation enforces the same account state and stored security version.
- [`RefreshToken`](../../ahis.template.identity/Models/Entities/RefreshToken.cs) stores only a SHA-256 hash and links each token to a random-public-ID session and, after rotation, its parent token. Device and network metadata remain intentionally absent pending product policy.
- [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) invalidates sessions after password change and authenticator disable; JWT validation consumes the resulting version.
- [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes initial password setup publicly only through a purpose-bound token flow; callers cannot select an account by user ID alone.
- [`AuthenticationController`](../../ahis.template.api/Controllers/v1/AuthenticationController.cs) no longer exposes token encode/decode or account-state diagnostic routes.
- [`Program.cs`](../../ahis.template.api/Program.cs) registers ASP.NET Core Identity roles, while [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) copies current user roles into issued JWTs.
- [`IdentityContext`](../../ahis.template.identity/Contexts/IdentityContext.cs) maps Identity roles, user-role assignments, and role claims, but no confirmed controller, mediator request, or Identity service contract manages them.
- The initial Identity migration contains evidence of both `AspNetRoles` and `IdentityRoles`; the effective mapping and foreign-key target require investigation before role persistence changes.

These are implementation observations, not owner-confirmed product requirements.
