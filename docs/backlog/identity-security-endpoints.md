# Identity Security Endpoint Backlog

## Purpose

This document tracks proposed security work for user identity, authentication, sessions, account recovery, and Identity administration. It is intended to make unimplemented work visible and to preserve the source evidence behind each proposal.

This is a backlog, not an approved implementation blueprint. Any item that changes a public API, authorization, security behavior, Identity persistence, schema, dependencies, or production configuration requires an approved change blueprint before implementation.

Last reviewed against source: 2026-09-21.

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
- [ ] Require the same invalidation in future MFA-reset, email-change, and account-deactivation workflows.
- [x] Apply the same active, deleted, lockout, and security-version checks during refresh-token rotation.
- [x] Adopt immediate access-token revocation through per-request version validation.

Implementation evidence (2026-09-21): bearer validation checks `IsActive`, `IsDeleted`, lockout, and a SecurityStamp-derived version. Refresh rotation applies the same checks. `RefreshTokens.SecurityVersion` was added by `AddRefreshTokenSecurityVersion`; existing refresh rows without a version fail closed. Migration application is user-reported and was not independently performed by Codex.

### Refresh-token storage and session model

- [ ] Store only a cryptographic hash of each refresh token.
- [ ] Introduce a non-sequential, opaque public session identifier.
- [ ] Record token family or parent/replacement relationships for replay detection.
- [ ] Record session creation, last-used, expiry, and revocation information.
- [ ] Decide whether to store bounded device name, client type, IP-derived information, and user-agent information.
- [ ] Define retention and cleanup for expired and revoked sessions.
- [ ] Add and review an `IdentityContext` migration; never apply it as routine verification.

Current evidence: `RefreshToken` stores the raw token with an integer ID, user ID, created/expiry timestamps, and revocation state.

### Existing endpoint hardening

- [ ] Replace public initial-password setup by user ID with a one-time, expiring, purpose-bound setup token.
- [ ] Remove token encode/decode diagnostic endpoints from production, or restrict them with a dedicated diagnostic policy and environment check.
- [ ] Bind two-factor login completion to an opaque, short-lived pre-authentication challenge instead of trusting a freely supplied user ID.
- [ ] Make forgot-password, resend-confirmation, and account-state responses consistently resistant to user enumeration.
- [ ] Standardize refresh-cookie name, path, `Secure`, `HttpOnly`, and `SameSite` behavior across login, 2FA, refresh, and logout.
- [ ] Ensure every public authentication and recovery endpoint has an explicit rate-limit decision.
- [ ] Stop separately serializing reusable recovery-code material into `ApplicationUser`; use the Identity token store as the source of truth.

## P1 — Recommended self-service endpoints

### Step-up authentication

- [ ] `POST /api/account/re-authenticate`
  - [ ] Require the current password.
  - [ ] Require a second factor when MFA is enabled.
  - [ ] Return a short-lived, purpose-constrained proof rather than a normal long-lived session.
  - [ ] Define which sensitive operations require the proof and its maximum age.
  - [ ] Rate-limit failures and audit successful and failed attempts without logging credentials or codes.

This is a dependency for session revocation, MFA reset, email change, and account deactivation.

### Revoke every session

- [ ] `POST /api/account/sessions/revoke-all`
  - [ ] Derive the user ID only from the authenticated principal.
  - [ ] Require a valid step-up proof.
  - [ ] Revoke all refresh-token sessions atomically.
  - [ ] Invalidate existing access tokens according to the approved token-version policy.
  - [ ] Define whether the current session is retained; default recommendation is to revoke it.
  - [ ] Clear the current refresh cookie when the current session is revoked.

The Identity service already has internal all-token revocation behavior, but no confirmed authenticated self-service endpoint exposes it.

### View active sessions

- [ ] `GET /api/account/sessions`
  - [ ] Return opaque session ID, creation time, last-used time, expiry, current-session indicator, and bounded device information.
  - [ ] Never return raw or hashed refresh-token values.
  - [ ] Return only sessions owned by the authenticated user.
  - [ ] Define how approximate location or IP information is masked and retained, if collected.

Blocked by the refresh-token storage and session-model work in P0.

### Revoke one session

- [ ] `DELETE /api/account/sessions/{sessionId}`
  - [ ] Verify ownership of the opaque session ID.
  - [ ] Require a recent step-up proof when revoking a session other than the current one.
  - [ ] Make repeated revocation idempotent.
  - [ ] Clear the refresh cookie when the current session is selected.
  - [ ] Audit the action without logging tokens.

Blocked by active-session listing and the P0 session model.

### Security summary

- [ ] `GET /api/account/security-summary`
  - [ ] Return email-confirmed, phone-confirmed, password-present, MFA-enabled, authenticator-configured, remaining-recovery-code count, and active-session count.
  - [ ] Never return authenticator keys, provisioning URIs, recovery codes, tokens, or password data.
  - [ ] Consider returning last password change and last security-sensitive event only if reliable timestamps exist.

### Regenerate recovery codes

- [ ] `POST /api/account/2fa/recovery-codes/regenerate`
  - [ ] Require MFA to be enabled and require recent step-up authentication.
  - [ ] Replace all previous recovery codes.
  - [ ] Return new codes exactly once.
  - [ ] Never log or persist a second plaintext copy of the codes.
  - [ ] Notify the user that recovery codes changed.

### Reset authenticator

- [ ] `POST /api/account/2fa/reset-authenticator`
  - [ ] Require a recent step-up proof or a separately approved recovery flow.
  - [ ] Disable the existing authenticator and invalidate old recovery codes.
  - [ ] Revoke other sessions and invalidate access tokens.
  - [ ] Require the normal authenticator setup and verification flow before re-enabling MFA.
  - [ ] Notify the user through a verified channel.

### Verified email change

- [ ] `POST /api/account/change-email/request`
  - [ ] Require a recent step-up proof.
  - [ ] Validate uniqueness without exposing whether another account owns the address.
  - [ ] Generate an ASP.NET Core Identity change-email token.
  - [ ] Send confirmation to the new address and a security notification to the old address.
  - [ ] Do not change the current email until confirmation succeeds.
- [ ] `POST /api/account/change-email/confirm`
  - [ ] Consume a one-time, expiring, purpose-bound token.
  - [ ] Update the normalized email through `UserManager`.
  - [ ] Explicitly decide whether username changes when username currently matches email.
  - [ ] Revoke sessions or update the token security version.
  - [ ] Return a generic error for invalid, expired, or already-used tokens.

### Account deactivation

- [ ] `POST /api/account/deactivate`
  - [ ] Require a recent step-up proof and explicit confirmation.
  - [ ] Use the existing inactive/soft-delete model according to an approved retention policy.
  - [ ] Revoke all refresh sessions and invalidate access tokens.
  - [ ] Prevent new login and refresh.
  - [ ] Notify the user and record a security audit event.
  - [ ] Decide whether deactivation is reversible and, if so, define the recovery path.

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

| Item | Status | Blueprint | Completed | Evidence/notes |
|---|---|---|---|---|
| P0 token and account-state enforcement | Implemented; future-flow hooks pending | Approved 2026-09-21 | 2026-09-21 | `AddRefreshTokenSecurityVersion` generated and user-reported as applied; MFA reset, email change, and deactivation do not yet exist |
| P0 refresh-token/session model | Backlog | — | — | — |
| P0 existing endpoint hardening | Backlog | — | — | — |
| Step-up authentication | Backlog | — | — | — |
| Revoke every session | Backlog | — | — | — |
| View active sessions | Blocked | — | — | Requires P0 session model |
| Revoke one session | Blocked | — | — | Requires active-session support |
| Security summary | Backlog | — | — | — |
| Regenerate recovery codes | Backlog | — | — | — |
| Reset authenticator | Backlog | — | — | — |
| Verified email change | Backlog | — | — | — |
| Account deactivation | Backlog | — | — | — |
| Account recovery | Backlog | — | — | Requires approved recovery policy |
| Administrative Identity controls | Backlog | — | — | Requires dedicated admin policy |
| User role management | Blocked | — | — | Requires role model decisions and P0 token invalidation |
| Passkey/WebAuthn support | Optional | — | — | Separate initiative |

## Source observations behind this backlog

- [`Program.cs`](../../ahis.template.api/Program.cs) validates JWT issuer, audience, lifetime, signing key, user existence, active/deleted state, lockout, and a SecurityStamp-derived version.
- [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) rotates refresh tokens and detects reuse; refresh validation enforces the same account state and stored security version.
- [`RefreshToken`](../../ahis.template.identity/Models/Entities/RefreshToken.cs) currently stores raw token material and has no opaque session ID, token family, last-used time, or device metadata.
- [`AccountService`](../../ahis.template.identity/Services/AccountService.cs) invalidates sessions after password change and authenticator disable; JWT validation consumes the resulting version.
- [`AccountController`](../../ahis.template.api/Controllers/v1/AccountController.cs) exposes initial password setup publicly and accepts a user ID in the request flow.
- [`AuthenticationController`](../../ahis.template.api/Controllers/v1/AuthenticationController.cs) exposes token encode/decode diagnostics without a confirmed authorization policy.
- [`Program.cs`](../../ahis.template.api/Program.cs) registers ASP.NET Core Identity roles, while [`AuthenticationService`](../../ahis.template.identity/Services/AuthenticationService.cs) copies current user roles into issued JWTs.
- [`IdentityContext`](../../ahis.template.identity/Contexts/IdentityContext.cs) maps Identity roles, user-role assignments, and role claims, but no confirmed controller, mediator request, or Identity service contract manages them.
- The initial Identity migration contains evidence of both `AspNetRoles` and `IdentityRoles`; the effective mapping and foreign-key target require investigation before role persistence changes.

These are implementation observations, not owner-confirmed product requirements.
