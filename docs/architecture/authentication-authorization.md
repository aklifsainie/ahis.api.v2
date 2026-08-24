# Authentication and Authorization

## JWT bearer authentication

The API validates issuer, audience, lifetime, and signing key. `ApplicationUser` must still exist, and a locked-out user is rejected during token validation. JWTs include name identifier, username, email, standard JWT claims, custom user claims, and roles.

Refresh tokens are persisted in `IdentityContext`. Successful refresh rotates the token. Reuse of a revoked token triggers revocation of all active refresh tokens for that user. Logout revokes the supplied usable refresh token and is idempotent at the command layer.

## API-key authentication

Clients send one `X-API-Key` header. The raw key is hashed with SHA-256 and compared with stored hashes. A key is valid only when it exists, is active, is not revoked or expired, and its client is active. Failure responses intentionally conceal the specific cause. Successful authentication adds client and permission claims and updates `LastUsedAt`.

The raw key is returned only during key creation. Never persist or log it.

## Authorization and rate limiting

Country accepts Bearer or API-key authentication and has a global API rate-limit attribute. `CountryReadPolicy` and `CountryWritePolicy` exist in `Program.cs`, but are not currently applied to Country endpoints. API-client administration currently has no authorization attribute. Treat both as known security risks, not as conventions to copy.

Authentication endpoints use the stricter `AuthPolicy` rate limiter selectively. API-client `RateLimitPerMinute` is stored but is not currently used to configure a per-client limiter.

## Configuration safety

Development settings currently contain plaintext operational secrets. Generated documentation and logs must name configuration keys only, never reproduce their values. Prefer environment variables, user secrets, or an approved secret store in future configuration work.
