# API Client Authentication Integration Points

- `ApiClientController`: administration HTTP boundary; currently lacks explicit authorization.
- `IApiClientService`/`ApiClientService`: creation, rotation, revocation, deactivation.
- `ApiKeyAuthenticationHandler`: reads `X-API-Key`, builds claims, returns ProblemDetails-like challenge/forbidden bodies.
- `IApiKeyValidator`/`ApiKeyValidator`: EF-backed validation and last-used update.
- `ApplicationDbContext` and API-key EF configurations.
- `ApiKeyPolicy`, `CountryReadPolicy`, and `CountryWritePolicy` in `Program.cs`.
- Country endpoints accept API-key authentication, although Country permission policies are currently not attached.

`RateLimitPerMinute` is persisted and returned by validation but the current global rate limiter does not consume it per client.
