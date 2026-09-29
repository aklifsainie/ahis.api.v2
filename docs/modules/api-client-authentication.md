# API-client Authentication Module

## Ownership and flow

The module owns API clients, generated API keys, permission claims, validation, revocation, rotation, and deactivation. Administration follows `ApiClientController -> IApiClientService -> ApplicationDbContext`. Runtime authentication follows `X-API-Key -> ApiKeyAuthenticationHandler -> IApiKeyValidator -> ApplicationDbContext -> claims principal`.

## Observed behavior

- Raw keys are returned only at creation; storage uses a SHA-256 hash and non-secret prefix.
- Client IDs and permissions are normalized; permissions are deduplicated and unique per client.
- Validation requires an active, non-revoked, unexpired key and active client, and fails generically.
- Client deactivation revokes active keys; repeat key revocation is idempotent.

## Authorization, auditing, and rate limits

API-client administration requires a bearer-authenticated current `Superadmin` and an actor-bound `X-Step-Up-Proof`; API keys cannot administer clients. Mutations write redacted `AuditLog` rows in the same application transaction. The direct controller-service path remains a local variation, not the default pattern.

Country traffic uses the client's persisted `RateLimitPerMinute` as a per-instance, per-client sliding-window quota shared by that client's keys. Invalid stored limits fail key authentication. `LastUsedAt` is best-effort telemetry, updated at most once per minute, and a tracking write failure does not reject a valid key. There are no committed HTTP integration tests.

These are observed implementation facts. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).
