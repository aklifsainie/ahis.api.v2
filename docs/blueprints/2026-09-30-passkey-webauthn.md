# Blueprint: optional passkey and WebAuthn support

**Status:** Proposed initiative blueprint. No dependency, framework upgrade, public API, schema, or relying-party configuration is authorized by this file alone. The decision gates below must be resolved in a reviewed revision before implementation.

## Outcome

Implement the six optional backlog routes as one coherent WebAuthn credential lifecycle: authenticated registration options/completion, owner-only listing/deletion, and anonymous authentication options/completion. Successful passkey authentication must enter the existing Identity account-state, restriction, session, refresh-cookie, JWT, rate-limit, and audit model. A passkey must never bypass account recovery or an administrator-required password reset.

## Owning module and analogous implementation

**Confirmed:** `AccountController` → custom mediator → `IAccountService` owns post-login account-security actions; `AuthenticationController` → mediator → `IAuthenticationService` owns login, 2FA, token issuance, refresh rotation, and cookie handling. `IdentityContext` owns users, refresh sessions/tokens, and recovery challenges. `AuthenticationService` creates an opaque session and issues the same token pair after completed password/2FA login. `Program.cs` configures bearer validation, cookies, restrictions, and rate limits. No passkey entity, handler, controller route, RP configuration, or WebAuthn library is present.

**External primary-source evidence checked 2026-09-30:** [Microsoft's ASP.NET Core 8 MFA guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/mfa?view=aspnetcore-8.0) says ASP.NET Core 8 does not directly support passkeys/FIDO2; [Microsoft's .NET 10 passkey guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0) describes built-in Identity support in .NET 10. The [W3C WebAuthn Level 3 specification](https://www.w3.org/TR/webauthn-3/) defines the challenge, RP ID, origin, user-verification and assertion checks. The [FIDO Alliance deployment guidance](https://fidoalliance.org/white-paper-displace-password-otp-authentication-with-passkeys/) recommends exercising registration, authentication, and recovery together.

## Observed flow and proposed flow

**Observed:** Password login may produce a short-lived 2FA challenge; completed authentication creates one refresh session and issues a bearer/refresh pair. Recovery uses purpose-bound challenge state and MFA evidence. JWT and refresh validation reject inactive, deleted, held, legacy-unclassified, locked, or stale-security-version users.

**Proposed staged flow:**

1. Decide the backend: remain on .NET 8 with a reviewed WebAuthn verifier dependency, or approve a separately scoped .NET 10 framework migration first. Record library version, maintenance/licensing, protocol support, and test vectors before coding.
2. Approve RP ID, exact allowed origins, HTTPS deployment hosts, reverse-proxy trust, credential policy, challenge TTL, attestation policy, user-verification requirement, per-user credential cap, and recovery policy. Reject a mismatch in any server-side verification step.
3. Add Identity-owned credential and one-time ceremony/challenge storage. Registration options bind a random challenge to the authenticated user and current security version; completion consumes it only after full attestation verification and unique credential-ID check. Listing returns safe labels/created and last-used times; deletion verifies ownership and step-up, blocks unsafe last-credential removal under the selected recovery policy, and revokes affected sessions if policy requires.
4. Authentication options create a bounded, single-use challenge without disclosing account existence. Completion verifies the assertion and stored public key, credential ownership, challenge, origin, RP ID, user presence/verification, signature, and counter/backup-state rules. It then checks current Identity eligibility and issues the existing JWT/refresh session pair using shared issuance code. Return the same generic failure for unknown credentials or invalid assertions.
5. Reconcile passkeys with MFA, step-up, forced-password-reset, logout, account deactivation, and recovery. Complete browser ceremony and API tests before exposing routes.

## Files to add, modify, or intentionally leave unchanged

- Add Account and Authentication passkey requests/handlers, public request/response models, Identity service contracts/implementation, credential/challenge entities and `IdentityContext` mappings, controller actions, DI registration, RP options, and focused tests.
- Modify token issuance only as needed to share existing session/JWT/cookie behavior; preserve bearer validation and hash-only refresh tokens. Add an Identity migration through `create-ef-migration` after explicit approval; no Application database migration is expected.
- Update Swagger, Account/Authentication module docs, security architecture, backlog, deployment configuration guidance, and user recovery runbook.
- Leave API-client keys, Country policies, arbitrary Identity role claims, and unrelated application persistence unchanged.

## Business behavior and confidence

**Confirmed invariant:** The six listed routes are optional backlog proposals and have no current implementation. The repo's Identity eligibility checks must apply to any new sign-in path. **Proposed default:** Passkeys are additional credentials on existing confirmed accounts, with user verification required; no passwordless account creation in the first release. A successful passkey assertion satisfies the sign-in credential ceremony, but whether it also satisfies existing MFA/step-up requirements needs explicit product/security approval. Listing and deletion are owner-only. Challenge expiry and consumption are enforced server-side, including simultaneous completion attempts.

## Authorization and security

Registration and listing require a valid bearer and active session; registration and deletion require recent actor-bound step-up by default. Anonymous authentication options/completion use strict per-IP and per-account/credential abuse limits and generic responses. Validate RP ID and origin against a fixed server-owned allow-list, never a request-supplied host. Require cryptographically random challenges, single-use persistence, supported algorithm allow-list, user presence and user verification per policy, and full attestation/assertion signature verification by the selected library. Store public keys and credential identifiers, never private keys or biometric data. Avoid raw assertion/challenge material in audit logs. Handle authenticator counters and synced-passkey backup flags according to the chosen verifier's documented threat model.

## API contract

| Route | Proposed behavior |
| --- | --- |
| `POST /api/account/passkeys/registration-options` | Return browser-compatible creation options for the authenticated account and a bounded challenge reference. |
| `POST /api/account/passkeys/registration-complete` | Verify and consume the registration ceremony; return safe credential metadata, without attestation secrets. |
| `GET /api/account/passkeys` | Return bounded owner-only credential summaries, never public-key bytes or raw attestation. |
| `DELETE /api/account/passkeys/{credentialId}` | Verify owner, proof, and last-credential policy; return no content on successful deletion. |
| `POST /api/authentication/passkeys/options` | Return browser-compatible request options with enumeration-resistant behavior. |
| `POST /api/authentication/passkeys/complete` | Verify assertion and current account state, then issue the existing token/cookie response shape. |

Specify precise JSON schemas, base64url encoding, challenge transport, status codes, cookie behavior, and response redaction in the reviewed implementation revision after backend choice. The HTTP API must not accept a freely supplied user ID as authentication proof.

## Entity, persistence, and migration impact

Add Identity-owned passkey credential rows with unique credential ID, user foreign key, public key, algorithm, sign counter, safe label, created/last-used times, and approved backup/attestation metadata. Add short-lived ceremony state keyed by a hash of an opaque challenge reference, bound to purpose, user when known, RP configuration, current security version when applicable, expiry and consumed time. Use unique indexes and bounded cleanup; model deletion/revocation and migration rollback carefully. Exact columns depend on the chosen verifier and policy, so do not generate a migration from this preliminary schema alone.

## Service decision and evidence

Add a dedicated Identity passkey service or a focused extension of existing Identity services, because WebAuthn verification, credential persistence, challenge consumption, and token issuance span several Identity operations. Controllers remain thin mediator dispatchers. Do not implement protocol cryptography by hand or move credential persistence into an Application repository. Keep token/session issuance behind the current authentication service boundary.

## Transaction, audit, and integration impact

Registration and assertion challenge consumption must be atomic with credential creation/update to prevent replay. Token issuance must happen only after assertion verification and a fresh account/restriction/security-version check. Audit fixed action, actor/target where known, credential row ID and outcome without logging challenges, signed client data, attestation, tokens, keys, or biometrics. Notification for add/delete and a verified recovery route must be approved. SMTP failure after a credential commit must not misreport the commit. RP configuration and verifier dependency are production changes requiring separate review.

## Test plan

Use official/library protocol vectors plus browser ceremony tests on allowed HTTPS origins. Cover wrong/replayed/expired challenge, origin/RP mismatch, malformed client data, wrong signature, unknown/duplicate credential, user-verification policy, counter anomalies, concurrent completion, wrong-owner list/delete, last-credential policy, rate limits, enumeration resistance, held/deleted/locked account denial, stale security version, forced-reset gate, session issuance/cookie settings, audit redaction, recovery, and cross-browser/device behavior. Test migration mapping and cleanup with a relational store.

## Documentation impact

After implementation, mark only delivered passkey routes in the backlog; update Account/Authentication/security/testing docs, Swagger contracts, deployment/RP configuration, privacy/retention statement, and recovery/support runbook. Add browser-client examples without embedding secrets.

## Validation commands

After a reviewed backend/contract revision and separate migration-generation approval: focused passkey protocol/HTTP tests, `dotnet restore .\AhisApiTemplate.sln`, `dotnet build .\AhisApiTemplate.sln --no-restore`, `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`, browser ceremony smoke tests, and migration SQL review. No routine database update, deployment, or framework upgrade.

## Risks, assumptions, and open questions

- **Blocking:** Stay on .NET 8 with an approved verifier dependency, or first migrate the solution to .NET 10? The latter is a separate broad framework/dependency change.
- **Blocking:** Confirm RP ID, allowed origins, proxy topology, attestation, user-verification and synced/device-bound passkey policy.
- **Blocking:** Decide whether passkey sign-in replaces or supplements TOTP, whether it counts as step-up, and how lost passkeys are recovered without weakening current MFA recovery.
- **Blocking:** Define passwordless-account and forced-password-reset interaction, credential cap, last-passkey deletion, notification guarantees, and privacy/retention.
- **Risk:** The six endpoints form one protocol lifecycle, but implementation should be staged and feature-gated until registration, authentication, deletion, and recovery are all safe together.
