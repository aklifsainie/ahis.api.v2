# Complete remaining administrative Identity controls and development passkeys

**Status:** Ready for approval. This revision consolidates the four remaining administrative Identity endpoints and the six optional passkey/WebAuthn endpoints. Approval authorizes only the implementation scope stated below. Migration generation, migration application, disposable SQL fixture creation/use/deletion, deployment, commit, and push remain separate gates.

## Outcome

Deliver the remaining backlog routes with fail-closed Identity behavior:

- `POST /api/admin/identity/users/{userId}/disable`
- `POST /api/admin/identity/users/{userId}/enable`
- `POST /api/admin/identity/users/{userId}/reset-mfa`
- `POST /api/admin/identity/users/{userId}/require-password-reset`
- `POST /api/account/passkeys/registration-options`
- `POST /api/account/passkeys/registration-complete`
- `GET /api/account/passkeys`
- `DELETE /api/account/passkeys/{credentialId}`
- `POST /api/authentication/passkeys/options`
- `POST /api/authentication/passkeys/complete`

Passkeys use the reviewed .NET 8 `Fido2` package version `4.3.0`; no preview package and no .NET framework upgrade is included. Initial passkey availability is development-only: RP ID `localhost`, allowed origins `https://localhost:7270` and `https://localhost:7280`. Production passkeys remain disabled and fail closed until a separate reviewed revision names its RP ID and allowed HTTPS origins.

## Owning modules and analogous flows

Administrative controls are owned by `AdminIdentityController` -> custom mediator -> Identity service boundary. Use the neighboring administrative security-state, revoke-sessions, and unlock flows for policy, actor-bound step-up, redacted audit, notification, transaction, and result mapping.

Passkey registration/list/delete are Account flows: `AccountController` -> custom mediator -> `IAccountService` or a dedicated narrow Identity-owned passkey service. Passkey authentication is an Authentication flow: `AuthenticationController` -> custom mediator -> `IAuthenticationService`, reusing the existing post-authentication session, refresh-cookie, and JWT issuance path. Controllers remain thin; WebAuthn verification and Identity persistence do not move into Application repositories.

## Confirmed decisions

### Administrative controls

- Every state-changing administrative route requires bearer-authenticated Superadmin authorization, the existing authenticated-security rate limit, current authoritative Superadmin membership, and a five-minute actor-bound `X-Step-Up-Proof` validated before target lookup.
- `disable` creates an active, durable `AdministrativeSecurityHold`, is idempotent when the same eligible target is already held, never disables the last effective Superadmin, invalidates target security version/tokens/sessions atomically, then attempts redacted audit and generic notification after commit.
- `enable` closes only an active `AdministrativeSecurityHold`. It never activates an inactive/deleted account or changes ordinary lockout, password, MFA, or other credential state.
- `reset-mfa` disables TOTP, resets the authenticator key, invalidates recovery codes, invalidates security version/tokens/sessions, and requires the user to complete normal authenticator setup and verification again.
- `require-password-reset` adds a durable `MustResetPassword` Identity state, invalidates security version/tokens/sessions, and blocks password, refresh, 2FA completion, step-up, and passkey sign-in until a successful existing password-reset flow clears the state.
- All state changes fail closed on inactive/deleted targets, unclassified restrictions, inconsistent lockout provenance, failed proof/current-role validation, failed Identity updates, failed token invalidation, concurrency conflict, or uncertain commit. Audit/notification failures after confirmed commit do not reverse success.

### Passkeys

- Passkeys are additional credentials only. They neither replace TOTP for accounts with TOTP enabled nor satisfy step-up authentication in this increment.
- Require user verification; request attestation format `none`; permit at most 10 active passkeys per account.
- Registration and authentication challenges are random, opaque, persisted as hashes, single-use, and expire after five minutes. Apply 5/minute IP limits and 5/minute account/credential limits whenever identity is known, with enumeration-resistant anonymous responses.
- Registration/listing are bearer-session owner-only. Registration and deletion require valid step-up proof. Deletion is allowed only if the account retains a password and another approved recovery/MFA path; otherwise reject removal of the final passkey.
- Anonymous assertion options/completion validate challenge, origin, RP ID, credential ownership, public-key signature, user presence/verification, supported algorithms, sign counter and backup state through `Fido2` 4.3.0. Invalid/unknown/replayed assertions receive generic responses.
- A `MustResetPassword` state blocks passkey authentication. Passkeys cannot bypass administrative password-reset intervention.

## Persistence and migrations

Identity-owned additions require model configuration and an explicitly approved `IdentityContext` migration:

- `ApplicationUser.MustResetPassword` with a safe default for existing users.
- Durable, redacted passkey credential storage: opaque credential ID, user ID, public key, algorithm, sign counter, safe display label, creation/last-used timestamps, and only necessary backup/attestation metadata.
- Short-lived passkey ceremony records with a hash of an opaque challenge reference, purpose, bound user when known, security-version binding when applicable, RP configuration identity, expiry, consumption time, and rate-limit key data that does not store raw identifying values.
- Constraints and indexes for unique credential IDs, bounded active credentials per user, unconsumed challenge handling, and owner lookups.

The migration must preserve current Identity tables and role-store state. Review `Up`, `Down`, designer, snapshot, and generated SQL before separate approval to apply. No `dotnet ef database update` is routine verification.

## API and error contract

Use existing response conventions, cancellation propagation, `Cache-Control: no-store` for sensitive results, and Swagger metadata.

Administrative routes return no sensitive target state. Preserve normal `401`, `403`, and `429`; perform proof checks before exposing target existence. Use generic invalid-target/proof outcomes, with `404` only after authorization and valid proof where the neighboring contract permits it.

Passkey options/completion use protocol-compatible payloads but do not accept a client-provided user ID as proof. Public keys, raw challenges, attestation objects, assertions, credential tokens, password-reset tokens, bearer tokens, refresh tokens, authenticator keys, and security stamps never enter responses, logs, audit metadata, or documentation.

## Security, transaction, audit, and notification

All privileged mutations coordinate Identity data and token/session invalidation in one Identity transaction. The durable restriction, password-reset state, and passkey/challenge records are authoritative for their respective behavior. Recheck target eligibility and effective-Superadmin protection inside the transaction; use provider-backed locking/serializable behavior for concurrent disables, role changes, deactivation, and restrictions.

Audit only fixed action names, actor/target IDs where appropriate, safe credential record IDs, and non-sensitive result categories. Send generic notifications for disable, enable, administrative MFA reset, required-password-reset, passkey added, and passkey removed only after a confirmed commit. Notification or best-effort audit failure must not expose data or alter a committed outcome.

## Files

Likely additions/modifications:

- `ahis.template.api/Controllers/v1/AdminIdentityController.cs`, `AccountController.cs`, `AuthenticationController.cs`, `Program.cs`
- `ahis.template.application/Features/AccountFeatures/` and `Features/AuthenticationFeatures/` requests, handlers, validators, and result mapping
- `ahis.template.identity/Interfaces/`, `Services/`, `Models/Entities/`, `Contexts/IdentityContext.cs`, `IdentityServiceRegistration.cs`, and `Migrations/`
- `ahis.template.test/TestFeatures/AccountFeature/`, `AuthenticationFeature/`, and `IdentityFeature/`
- `ahis.template.api/ahis.template.api.csproj` or the narrowly owning project for `Fido2` 4.3.0
- documentation: backlog, Account/Authentication/Identity modules, authentication-authorization, persistence, testing strategy, deployment configuration guidance, and user recovery runbook.

Do not modify API-client authentication, Country persistence, arbitrary role claims, production configuration values, the operational bootstrap CLI target, or unrelated migrations.

## Test plan

Write focused failing tests before production code for each behavior. Cover controller authorization and step-up ordering; actor/target separation; protected-role and last-effective-Superadmin races; restriction/lockout consistency; idempotency; token/session/proof invalidation; rollback; audit/notification redaction; and no-store API responses.

For passkeys, add official `Fido2` vectors and API/ceremony tests for invalid/replayed/expired challenge, origin/RP mismatch, malformed client data, wrong signature, unknown or duplicate credential, user-verification policy, counter/backup behavior, concurrent completion, owner-only list/delete, final-passkey policy, rate limits, enumeration resistance, account restriction/deletion/lockout/password-reset denial, post-authentication session/cookie issuance, and redacted audit/notification. SQL Server relational tests are required for persistence, unique constraints, transaction and concurrency properties; mocks alone are insufficient.

Run focused tests, then affected-project build, solution build, and full test suite. The existing LocalDB suite needs a separately approved disposable SQL Server test fixture when LocalDB is unavailable. Never point tests at an operational database.

## Documentation and rollout

Update the backlog only after source, tests, and relevant documentation are verified. Keep passkeys development-only behind the fixed localhost configuration; production configuration requires a new reviewed revision, explicit production RP/origin approval, deployment review, and browser ceremony validation on the real HTTPS origins.

## Risks and remaining gates

- Existing role-store mapping/test disagreement must be resolved before administrative operations rely on Superadmin membership as a concurrency invariant.
- The existing `AddIdentityUserRestrictions` migration and its provenance rollout must be verified in the target environment before disable/enable are exposed.
- Fido2 package addition, model/migration edits, migration generation, SQL fixture lifecycle, migration application, deployment, commit, and push require the separate approvals stated at the top of this blueprint.
- No database target, credentials, migration history, deployed role membership, production RP ID, or production allowed origin is established by this plan.
