# Identity Security Endpoint Backlog

## Purpose

This document tracks proposed security work for user identity, authentication, sessions, account recovery, and Identity administration. It is intended to make unimplemented work visible and to preserve the source evidence behind each proposal.

This is a backlog, not an approved implementation blueprint. Any item that changes a public API, authorization, security behavior, Identity persistence, schema, dependencies, or production configuration requires an approved change blueprint before implementation.

Last reviewed against source: 2026-09-30. Database application status is based on user reports, not independent inspection.

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
- [x] Record authoritative Identity restriction provenance and deny holds or unclassified legacy restrictions across login, refresh, bearer, two-factor, and step-up validation (2026-09-25; migration generated and source-reviewed; current database application status not independently verified). Administrative hold placement, release, and legacy classification remain internal-only pending their separately approved operation/endpoints.
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
- [x] Add and review an `IdentityContext` migration; never apply it as routine verification. [`ReplaceRefreshTokensWithHashedSessionModel`](../../ahis.template.identity/Migrations/20260921074330_ReplaceRefreshTokensWithHashedSessionModel.cs) clears legacy raw-token rows before adding the required hash/session columns, so deployment requires users to sign in again. The migration was generated and source-reviewed; current database application status was not independently verified. (2026-09-21)

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

Implementation evidence (2026-09-21): [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) creates and validates a 30-minute `InitialPasswordSetup` Identity token and uses generic recovery responses. [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) issues a five-minute protected two-factor challenge, validates it before completing MFA, and clears it after use or failure. [`RefreshCookie`](../../ahis.template.api/Security/RefreshCookie.cs) centralizes the canonical `refresh_token` cookie settings and cleanup. [`Program.cs`](../../ahis.template.api/Program.cs) configures partitioned anonymous, authenticated, and refresh rate-limit policies. [`RemoveApplicationUserRecoveryCodes`](../../ahis.template.identity/Migrations/20260921082113_RemoveApplicationUserRecoveryCodes.cs) removes the duplicate recovery-code column; it was generated and source-reviewed, with current database application status not independently verified.

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

Implementation evidence (2026-09-22): [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes the authenticated, rate-limited endpoint. [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) validates the proof and uses `IIdentityTokenStateService.InvalidateAsync` inside an Identity transaction. [`RevokeAllSessionsCommandHandlerTest`](../../ahis.template.test/TestFeatures/AccountFeature/RevokeAllSessionsCommandHandlerTest.cs) covers current-principal routing and success auditing. The API and Infrastructure EF Core references were aligned to `8.0.22`, matching Identity, after a production `MissingMethodException` from mixed EF Core 8/9 `ExecuteUpdateAsync` signatures. At that time, focused tests (2) and the full suite (16) passed. The 2026-09-30 [LocalDB concurrency tests](../../ahis.template.test/TestFeatures/IdentityFeature/IdentityConcurrencyTest.cs) now cover refresh versus revoke-all and revoke-one; other interleavings and deployment engines remain unverified.

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

- [x] `POST /api/account/2fa/recovery-codes/regenerate` (2026-09-22)
  - [x] Require MFA to be enabled and require recent step-up authentication.
  - [x] Replace all previous recovery codes.
  - [x] Return new codes exactly once.
  - [x] Never log or persist a second plaintext copy of the codes.
  - [x] Notify the user that recovery codes changed.

Implementation evidence (2026-09-22): [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes the authenticated, rate-limited endpoint with a no-store response. [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) validates the user-bound proof and uses the ASP.NET Core Identity token store as the sole recovery-code store. [`RegenerateRecoveryCodesCommandHandlerTest`](../../ahis.template.test/TestFeatures/AccountFeature/RegenerateRecoveryCodesCommandHandlerTest.cs) covers principal routing and audit behavior without recording recovery-code material. Manual Swagger verification confirmed the login, MFA, step-up, and regeneration flow; the generated codes were returned successfully and the endpoint behavior matched the documented contract.

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

- [x] `POST /api/account/recovery/start` (2026-09-22)
  - [x] Return the same response whether or not the account exists.
  - [x] Apply strict per-IP and per-account rate limits.
  - [x] Issue only a short-lived, single-use, purpose-bound recovery challenge.
  - [x] Notify the user through already verified channels.
- [x] `POST /api/account/recovery/complete` (2026-09-22)
  - [x] Require the approved recovery evidence.
  - [x] Prevent bypass of stronger MFA without an explicit recovery policy.
  - [x] Reset the affected credential, revoke all sessions, and invalidate access tokens.
  - [x] Notify the user of the completed recovery.
  - [x] Record sufficient audit evidence without storing secrets.

Implementation evidence (2026-09-22): [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes anonymous, recovery-rate-limited start and complete actions. [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) stores hashes only, binds each challenge to the current security version, requires authenticator or recovery-code evidence when MFA is enabled, and invalidates sessions after the password update commits. [`AddAccountRecovery`](../../ahis.template.identity/Migrations/20260922081341_AddAccountRecovery.cs) was generated and source-reviewed; current database application status was not independently verified. Deployment requires `Identity:PublicClientBaseUrl` and a secret `Identity:RecoveryHashKey`.

## P2 — Administrative Identity controls

All administrative endpoints require a dedicated administrative authorization policy, actor/target audit records, and protection against an administrator modifying their own access in an unsafe way.

- [x] `GET /api/admin/identity/users/{userId}/security-state` (2026-09-25)
  - [x] Requires `IdentityAdminSecurityReadPolicy` and the `Superadmin` role. It uses the configured default bearer authentication scheme; unlike the other two admin policies, this policy does not name the scheme explicitly.
  - [x] Returns only account status, confirmation, credential-presence, MFA, recovery-code-count, and active-session-count state; it excludes credentials, tokens, authenticator material, security stamps, and audit values.
  - [x] Audits successful reads with the target user ID and fixed non-sensitive metadata.
- [x] `POST /api/admin/identity/users/{userId}/revoke-sessions` (2026-09-25)
  - [x] Requires the bearer-authenticated `IdentityAdminSessionRevocationPolicy` for the `Superadmin` role, the authenticated-security rate limit, and a five-minute actor-bound step-up proof.
  - [x] Validates the proof before target lookup; a missing target is `404` only after authorization and proof validation. Existing inactive, deleted, or locked targets remain revocable.
  - [x] Rotates the target security version and revokes refresh tokens and sessions atomically; it is idempotent, sends no notification, and attempts a redacted target-scoped audit without changing a committed result if delivery fails.
- [x] `POST /api/admin/identity/users/{userId}/unlock` (2026-09-25)
  - [x] Requires bearer-authenticated `IdentityAdminUserUnlockPolicy`, current Superadmin role membership, the authenticated-security rate limit, and a five-minute actor-bound step-up proof. Self-unlock is rejected.
  - [x] Closes only a current `OrdinaryLockout` provenance row that agrees with Identity's future `LockoutEnd`; holds, unclassified restrictions, legacy mismatches, expired lockouts, inactive/deleted targets, and disabled lockout are refused. A clean target without a restriction or `LockoutEnd` is idempotent.
  - [x] An actual unlock clears the lockout end and failed count, retains the closed provenance record with actor/time, atomically invalidates target credential state, and attempts a generic notification and redacted actor/target audit after commit.
- [ ] `POST /api/admin/identity/users/{userId}/disable`
- [ ] `POST /api/admin/identity/users/{userId}/enable`
- [ ] `POST /api/admin/identity/users/{userId}/reset-mfa`
- [ ] `POST /api/admin/identity/users/{userId}/require-password-reset`

Administrative responses must never contain password hashes, authenticator secrets, raw recovery codes, raw refresh tokens, reset tokens, signing keys, or unmasked sensitive audit values.

## P2 — User role management

ASP.NET Core Identity role storage and JWT role claims are managed through the approved v1 role-catalog and direct user-role endpoints. Allow-listed human-user permissions are implemented through custom-role claims; arbitrary role-claim editing remains deferred.

Role management should remain in the Identity boundary and use `RoleManager<IdentityRole>` and `UserManager<ApplicationUser>`. It must not reuse API-client permissions, which belong to the separate API-client authentication module and use a different principal and claim type.

### Role authorization model

- [x] Define the initial role catalog and identify protected system roles: `Superadmin` and `User` are seeded and protected.
- [x] Decide whether roles alone are sufficient or whether human-user permissions will be represented by Identity role claims: v1 supports allow-listed human-user permissions on custom roles through Identity role claims. (Confirmed in source 2026-09-30)
- [x] Define dedicated policies for reading roles, managing roles, and assigning roles: `IdentityRoleReadPolicy`, `IdentityRoleManagementPolicy`, and `IdentityRoleAssignmentPolicy` require bearer-authenticated `Superadmin`.
- [x] Define a secure first-administrator bootstrap process that is unavailable after bootstrap: an existing active, confirmed, unrestricted user is selected by deployment configuration and guarded by `IdentityRoleBootstrapState`.
- [x] Prevent deletion or unsafe renaming of protected system roles.
- [x] Prevent removal of the last effective administrator during direct role replacement; synchronized cross-demotion tests passed on SQL Server 2019 LocalDB on 2026-09-30. Other interleavings and deployment engines remain unverified.
- [x] Prohibit administrators from changing their own direct role set.
- [x] Invalidate affected users' token/session state after direct membership changes and assigned-role renames.
- [x] Consolidate the Identity role store: the configured role type maps to `IdentityRole`, with user-role and role-claim relationships referencing it. The user reports applying `ConsolidateIdentityRoleStore` on 2026-09-29; other environments require their own inventory.

The P0 token/security-version mechanism is invoked for affected users because current JWTs contain a snapshot of roles and human-user permissions at issuance. Only allow-listed `human_permission` role claims are exposed for mutation.

Implementation evidence (2026-09-29): [`IdentityRoleBootstrapService`](../../ahis.template.identity/Services/IdentityRoleBootstrapService.cs) seeds the protected `Superadmin` and `User` roles and performs one-time first-administrator promotion from `Identity:Bootstrap:SuperadminEmail`. [`IdentityRoleManagementService`](../../ahis.template.identity/Services/IdentityRoleManagementService.cs) owns `RoleManager`, `UserManager`, transactions, proof checks, direct assignment changes, and token-state invalidation. [`AdminIdentityRolesController`](../../ahis.template.api/Controllers/v1/AdminIdentityRolesController.cs) and [`AdminIdentityController`](../../ahis.template.api/Controllers/v1/AdminIdentityController.cs) expose the routes. Selected HTTP policy and LocalDB concurrency cases were added on 2026-09-30; see the [test runbook](../guides/identity-security-test-runbook.md) for exact coverage and limits.

### Role catalog endpoints

- [x] `GET /api/admin/identity/roles`
  - [x] Require the role-read policy.
  - [x] Support bounded pagination and optional normalized-name filtering.
  - [x] Return role ID, name, protected status, and assignment count without concurrency values.
- [x] `GET /api/admin/identity/roles/{roleId}`
  - [x] Return role details with the current allow-listed permissions collection and opaque ETag. (Confirmed in source 2026-09-30)
  - [x] Return a consistent not-found response without unrelated user data.
- [x] `POST /api/admin/identity/roles`
  - [x] Require the role-management policy and recent step-up authentication.
  - [x] Normalize names through `RoleManager`; reject blank, duplicate, reserved, or invalid names.
  - [x] Attempt a redacted post-commit audit without proof or credential material.
- [x] `PUT /api/admin/identity/roles/{roleId}`
  - [x] Require the role-management policy, recent step-up authentication, and `If-Match`.
  - [x] Use opaque ETag optimistic concurrency.
  - [x] Protect system-role names and invalidate assigned users after a custom-role rename.
- [x] `DELETE /api/admin/identity/roles/{roleId}`
  - [x] Require the role-management policy and recent step-up authentication.
  - [x] Reject protected roles and assigned roles; repeated deletion is `404`.

### User-role assignment endpoints

- [x] `GET /api/admin/identity/users/{userId}/roles`
  - [x] Require the role-read policy.
  - [x] Return direct Identity role assignments only.
- [x] `PUT /api/admin/identity/users/{userId}/roles`
  - [x] Require the role-assignment policy and recent step-up authentication.
  - [x] Treat submitted role IDs as an atomic desired set and validate all IDs before writes.
  - [x] Prevent direct self-role changes and removal of the last effective administrator.
  - [x] Update the target security version and revoke refresh sessions after a changed set.
- [x] Expand the redacted audit metadata to include added and removed role IDs. A changed set records only the fixed action and sorted role-ID arrays after commit; a no-op or failed replacement writes no mutation audit. (2026-09-29)
- [x] `GET /api/admin/identity/roles/{roleId}/users`
  - [x] Require the role-read policy.
  - [x] Return a bounded, paginated projection rather than full Identity user records.

An atomic `PUT` is recommended for assignment because it gives the server one validated target state. Separate `POST .../roles/{roleId}` and `DELETE .../roles/{roleId}` operations may be used instead if the product requires granular changes, but the API should not expose both styles without a clear consistency rule.

### Optional human-user permission endpoints

The approved human-user permission model adds these endpoints alongside roles:

- [x] `GET /api/admin/identity/roles/{roleId}/permissions`
- [x] `PUT /api/admin/identity/roles/{roleId}/permissions`
- [x] `GET /api/admin/identity/permissions` returns the server-owned permission catalog under `IdentityRoleReadPolicy`. (Confirmed in source 2026-09-30; [controller](../../ahis.template.api/Controllers/v1/AdminIdentityPermissionsController.cs), [handler](../../ahis.template.application/Features/IdentityRoleManagementFeatures/IdentityRoleManagementRequests.cs), [approved blueprint](../blueprints/2026-09-29-p2-human-user-permissions.md))
- [x] Define a canonical, allow-listed permission catalog rather than accepting arbitrary claim types or values: `country.read` and `country.write`.
- [x] Emit human-user permissions as `human_permission`, distinct from API-client `api_permission` claims.
- [x] Invalidate authorization state for all assigned users after a permission change.
- [x] Add policy-level tests proving that roles and permissions are applied to protected actions. [HTTP tests](../../ahis.template.test/TestFeatures/IdentityFeature/IdentityPolicyTest.cs) passed on 2026-09-30 with real bearer middleware and API-key scheme selection; they cover administrator reads, Country read/write, direct-claim exclusion on real login, token invalidation, and bearer precedence. The [runbook](../guides/identity-security-test-runbook.md) identifies test-only mediator responses and remaining coverage limits.

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

Apply this checklist to each future endpoint separately; the unchecked boxes here are a review template, not a status report for endpoints already marked implemented:

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
| P0 refresh-token/session model | Implemented in source; deployment unverified | Approved 2026-09-21 | 2026-09-21 | Hashed rotation lineage, opaque sessions, replay invalidation, and daily cleanup exist. `ReplaceRefreshTokensWithHashedSessionModel` application was not independently verified. |
| P0 existing endpoint hardening | Implemented in source; deployment unverified | Approved 2026-09-21 | 2026-09-21 | Purpose-bound password setup, protected 2FA challenge, canonical refresh cookie, and public rate limits exist. `RemoveApplicationUserRecoveryCodes` application was not independently verified. |
| Step-up authentication                 | Implemented | Approved 2026-09-21 | 2026-09-21 | Five-minute proof bound to the user and current security version                                                                                                                                                                                                                                                                                                                                        |
| Revoke every session                   | Implemented | Approved 2026-09-22 | 2026-09-22 | [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) revokes the authenticated user's current and other sessions with a step-up proof; the Identity security version invalidates access tokens and proofs. EF Core references were aligned to `8.0.22` after a mixed-version runtime failure. The 2026-09-30 LocalDB suite passed the tested refresh/revoke-all race; broader interleavings remain unverified. |
| View active sessions                   | Implemented (metadata deferred) | Approved 2026-09-22 | 2026-09-22 | Authenticated, paginated owner-only listing of active sessions; access-token session claim identifies the current row. Device, IP, and location collection remain deferred pending privacy and retention policy. |
| Revoke one session | Implemented | — | 2026-09-22 | `DELETE /api/account/sessions/{sessionId}` is implemented in [AccountController](../../ahis.template.api/Controllers/v1/AccountController.cs) and [AccountService](../../ahis.template.identity/Services/AccountService.cs); [handler tests](../../ahis.template.test/TestFeatures/AccountFeature/RevokeSessionCommandHandlerTest.cs) cover routing and proof forwarding. |
| Security summary | Implemented | — | 2026-09-22 | `GET /api/account/security-summary` is implemented in [AccountController](../../ahis.template.api/Controllers/v1/AccountController.cs) and [AccountService](../../ahis.template.identity/Services/AccountService.cs); [handler tests](../../ahis.template.test/TestFeatures/AccountFeature/GetSecuritySummaryQueryHandlerTest.cs) cover projection and routing. |
| Regenerate recovery codes              | Implemented | Approved 2026-09-22 | 2026-09-22 | Authenticated step-up-protected regeneration returns recovery codes once, replaces the Identity token-store values, emits a no-store response, and sends a best-effort security notification. |
| Reset authenticator                    | Implemented | Approved 2026-09-21 | 2026-09-21 | Requires step-up proof and invalidates credentials                                                                                                                                                                                                                                                                                                                                                      |
| Verified email change                  | Implemented | Approved 2026-09-21 | 2026-09-21 | Uses Identity change-email token and invalidates credentials at confirmation                                                                                                                                                                                                                                                                                                                            |
| Account deactivation                   | Implemented | Approved 2026-09-21 | 2026-09-21 | Inactive and soft-delete state with no self-service reversal                                                                                                                                                                                                                                                                                                                                            |
| Account recovery | Implemented in source; deployment unverified | Approved 2026-09-22 | 2026-09-22 | Recovery start and completion are implemented. `AddAccountRecovery` exists; its application and required configuration were not independently verified in the target database. |
| Administrative Identity controls       | Partially implemented | Approved 2026-09-25 | 2026-09-25 | `GET /api/admin/identity/users/{userId}/security-state` requires `IdentityAdminSecurityReadPolicy` and the `Superadmin` role, returns a redacted security-state projection, and records a target-scoped view audit. `POST /api/admin/identity/users/{userId}/revoke-sessions` requires a dedicated bearer `Superadmin` policy and current actor-bound proof, then atomically invalidates target access and refresh sessions without notification; post-commit target-scoped auditing is best effort. `POST /api/admin/identity/users/{userId}/unlock` requires a dedicated bearer `Superadmin` policy and actor-bound proof, rejects self-unlock and non-current ordinary lockouts, and records the closed restriction plus post-commit notification/audit attempts. Role-store consolidation, protected role seeding, dedicated RBAC policies, one-time administrator bootstrap, role catalog, and direct assignment endpoints are implemented. |
| Role-store consolidation | Implemented in source; user-reported applied | [2026-09-29 revision](../blueprints/2026-09-29-rbac-role-store-consolidation-revision.md) | 2026-09-29 | `IdentityRole` is the single configured role entity and table; the guarded migration was reported applied to one database, without independent inspection. |
| RBAC foundation and bootstrap | Implemented in source; user-reported migration applied | [2026-09-29 foundation](../blueprints/2026-09-29-rbac-foundation-bootstrap.md) | 2026-09-29 | Protected `Superadmin` and `User` roles, three dedicated bearer policies, durable one-time bootstrap marker, and existing-user promotion are implemented. The user reports successful database application and bootstrap testing. |
| User role management | Implemented in source; manually and automatically verified in selected cases | [2026-09-30 human permissions](../blueprints/2026-09-29-p2-human-user-permissions.md) | 2026-09-30 | Role catalog, direct assignments, and custom-role `country.read`/`country.write` permissions use the dedicated Identity service. Permission writes require bearer Superadmin authorization, actor-bound step-up proof, and role ETag; they invalidate assigned users' access and refresh sessions and audit sorted permission deltas. The user reports successful execution of the human-user permission runbook. The 2026-09-30 [test runbook](../guides/identity-security-test-runbook.md) records passing HTTP policy, JWT provenance/invalidation, and selected LocalDB demotion/permission races; other interleavings remain unverified. |
| Identity security policy/concurrency verification | Selected cases verified on LocalDB | [Approved 2026-09-30](../blueprints/2026-09-30-identity-security-pending-verification.md) | 2026-09-30 | [Runbook](../guides/identity-security-test-runbook.md): 32 focused and 94 full-suite tests passed on SQL Server 2019 LocalDB 15.0.4382.1. No deployed database, alternate SQL Server engine, or all possible race schedules were verified. |
| Passkey/WebAuthn support               | Optional    | —                   | —          | Separate initiative                                                                                                                                                                                                                                                                                                                                                                                     |

## Source observations behind this backlog

- [`Program.cs`](../../ahis.template.api/Program.cs) validates JWT issuer, audience, lifetime, signing key, user existence, active/deleted state, lockout, and a SecurityStamp-derived version.
- [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) rotates hash-addressed refresh tokens, preserves parent relationships, updates session activity, and invalidates user token state on replay; refresh validation enforces the same account state and stored security version.
- [`RefreshToken`](../../ahis.template.identity/Models/Entities/RefreshToken.cs) stores only a SHA-256 hash and links each token to a random-public-ID session and, after rotation, its parent token. Device and network metadata remain intentionally absent pending product policy.
- [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) invalidates sessions after password change and authenticator disable; JWT validation consumes the resulting version.
- [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes initial password setup publicly only through a purpose-bound token flow; callers cannot select an account by user ID alone.
- [`AuthenticationController`](../../ahis.template.api/Controllers/v1/AuthenticationController.cs) no longer exposes token encode/decode or account-state diagnostic routes.
- [`Program.cs`](../../ahis.template.api/Program.cs) registers ASP.NET Core Identity roles and authorization policies, while [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) copies current user roles and allow-listed human-user permissions into issued JWTs.
- [`IdentityContext`](../../ahis.template.identity/Contexts/IdentityContext.cs) maps Identity roles, user-role assignments, and role claims. [`AdminIdentityRolesController`](../../ahis.template.api/Controllers/v1/AdminIdentityRolesController.cs), [`AdminIdentityController`](../../ahis.template.api/Controllers/v1/AdminIdentityController.cs), and [`AdminIdentityPermissionsController`](../../ahis.template.api/Controllers/v1/AdminIdentityPermissionsController.cs) expose the implemented role, assignment, and allow-listed permission operations through mediator requests and [`IIdentityRoleManagementService`](../../ahis.template.identity/Interfaces/IIdentityRoleManagementService.cs).
- The initial Identity migration created both `AspNetRoles` and `IdentityRoles`. `ConsolidateIdentityRoleStore` maps the configured role type to `IdentityRole` and removes the empty duplicate table; application to one database was reported on 2026-09-29 but not independently verified.

These are implementation observations, not owner-confirmed product requirements.
