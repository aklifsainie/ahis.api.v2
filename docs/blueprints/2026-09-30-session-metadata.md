# Blueprint: bounded session metadata

**Status:** Proposed for product and security review. Implementation is gated on the privacy and retention decisions below. This blueprint covers the deferred metadata item in `docs/backlog/identity-security-endpoints.md` only.

## Outcome

Give users useful context for identifying sessions in `GET /api/account/sessions` while collecting the minimum approved metadata. Keep the current opaque session ID, owner-only pagination, and token-free response. Device/client description, network information, and location are separable fields: approve each before adding it.

## Owning module and analogous implementation

**Confirmed:** `AccountController.GetActiveSessions` → `GetActiveSessionsQueryHandler` → `IAccountService.GetActiveSessionsAsync` → no-tracking `IdentityContext.RefreshSessions` projection. `AuthenticationService.StoreRefreshTokenAsync` creates a `RefreshSession` at completed login or 2FA and reuses it on rotation. `RefreshSession` has lifecycle fields but no device or network fields. `IdentitySessionCleanupService` deletes ended sessions on the documented retention schedule. Public session view models live under Domain; internal session DTOs are in Identity.

## Observed flow and proposed flow

**Observed:** Sessions are created with random public IDs and UTC times, then returned only to their owner. No client-supplied display name or IP is stored. The backlog explicitly defers metadata pending a privacy and retention policy.

**Proposed:** Capture approved metadata only when a session is created, not on every refresh. Pass a bounded, server-normalized capture object from the HTTP entry point through the mediator/Identity service boundary; never trust a client-supplied IP header unless a configured trusted-proxy chain has been established. Store only the approved fields on `RefreshSession` and add nullable, bounded response fields. Existing rows show `null` or an explicit unavailable value. Keep the no-tracking owner predicate and current-session calculation.

## Files to add, modify, or intentionally leave unchanged

- Modify `RefreshSession`, `IdentityContext` mapping, login and two-factor completion request/handler/service paths, `IAccountService.GetActiveSessionsAsync`, `ActiveSessionDto`, `ActiveSessionResponseVM`, and the session query mapping.
- Modify the relevant controller/API capture boundary only after deciding trusted proxy behavior. Add one `IdentityContext` migration through `create-ef-migration` after separate explicit migration-generation approval.
- Extend focused service, handler, HTTP, and cleanup tests; update Account/Authentication module guidance and API remarks.
- Leave refresh-token hashes, session public IDs, bearer validation, API-client metadata, and the Application database unchanged.

## Business behavior and confidence

**Confirmed invariant:** Session listing returns only active sessions belonging to the authenticated user and never returns refresh-token material. A session represents completed authentication, so failed login and incomplete 2FA must not create metadata rows.

**Proposed default:** Store at most a short sanitized client/device label and a coarse IP-derived value only if product approves both; location requires a separate data source and explicit policy, so it remains out of the first implementation slice. Do not present a User-Agent string as a verified device identity. Values must be bounded, safe to render, and optional.

## Authorization and security

Reuse the authenticated owner-only session endpoint and `AuthenticatedSecurityPolicy`. Treat User-Agent and any client name as untrusted text; strip control characters, cap length, and encode in clients. Trust `RemoteIpAddress` only under reviewed forwarding configuration. Avoid raw IP and precise geolocation unless the owner specifies necessity, lawful basis, access scope, and retention. Do not add metadata to audit payloads or logs. No new privilege is conveyed by metadata.

## API contract

`GET /api/account/sessions` keeps its existing envelope, pagination and lifecycle fields; add only approved nullable fields with stable documented names. Old rows and clients must remain valid. `DELETE /api/account/sessions/{sessionId}` remains keyed solely by opaque session ID. Define a response example after the product chooses the fields; do not expose raw User-Agent, token data, or location coordinates by default.

## Entity, persistence, and migration impact

An additive, nullable `IdentityContext.RefreshSessions` migration is expected for approved fields, with explicit max lengths and no backfill of historical rows. Retention should use the existing ended-session cleanup; if policy requires a shorter metadata lifetime than the session lifetime, add a separate purge operation or omit that field. Review migration SQL and rollback/data-loss behavior before generation and separately authorize application per environment.

## Service decision and evidence

Extend `AuthenticationService` and `AccountService`, since Identity already owns session creation/listing. Do not add an Application repository around `IdentityContext`. A small capture contract may be shared with Application without moving persistence ownership.

## Transaction, audit, and integration impact

Metadata should be saved in the same Identity transaction as a newly completed session. Listing remains no-tracking. Existing explicit session-view audit should record count only. Location enrichment, if later approved, must have its own integration and failure policy; a geolocation lookup must not block sign-in by default.

## Test plan

Cover successful login and 2FA capture, failed/incomplete login exclusion, refresh preserving original metadata, owner-only projection, old null rows, maximum length/control-character sanitization, forged forwarding headers, missing IP/User-Agent, and cleanup. Add migration mapping verification with a relational fixture. Check response and audit redaction.

## Documentation impact

Update the backlog's metadata checkbox only for fields actually delivered, `docs/modules/account.md`, `docs/modules/authentication.md`, `docs/architecture/authentication-authorization.md`, and Swagger remarks. Document collection and retention in the product privacy notice outside this repository if required by its owner.

## Validation commands

After approval and migration-generation approval: focused session tests, `dotnet restore .\AhisApiTemplate.sln`, `dotnet build .\AhisApiTemplate.sln --no-restore`, `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`, and migration SQL review. No routine database update.

## Risks, assumptions, and open questions

- **Blocking product decisions:** Which exact fields are needed, why, who may see them, and for how long? Is coarse IP-derived information acceptable? Is location required, and if so from which approved provider?
- **Blocking infrastructure decision:** Which proxies are trusted, and how are forwarded headers configured in each environment?
- **Risk:** Device labels and IP-derived values can mislead users; present them as hints, never authentication evidence.
- **Assumption:** The existing session-retention window is acceptable for approved metadata unless product policy specifies a shorter one.
