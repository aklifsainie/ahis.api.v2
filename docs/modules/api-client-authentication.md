# API-client Authentication Module

## Ownership and flow

The module owns API clients, generated API keys, permission claims, validation, revocation, rotation, and deactivation. Administration follows `ApiClientController -> IApiClientService -> ApplicationDbContext`. Runtime authentication follows `X-API-Key -> ApiKeyAuthenticationHandler -> IApiKeyValidator -> ApplicationDbContext -> claims principal`.

## Observed behavior

- Raw keys are returned only at creation; storage uses a SHA-256 hash and non-secret prefix.
- Client IDs and permissions are normalized; permissions are deduplicated and unique per client.
- Validation requires an active, non-revoked, unexpired key and active client, and fails generically.
- Client deactivation revokes active keys; repeat key revocation is idempotent.

## Risks and boundaries

Administration lacks explicit authorization and mutation audit coverage. The direct controller-service path is an established local variation, not the default pattern. `RateLimitPerMinute` is persisted but not consumed by a client-specific limiter. There are no committed module tests.

These are observed implementation facts. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).
