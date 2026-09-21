# Authentication and Authorization

JWT validation checks issuer, audience, lifetime, signing key, user existence, and lockout. Password login additionally rejects inactive/deleted users and uses Identity lockout handling. These are observed implementation behaviors: current JWT validation and refresh rotation do not re-check active/deleted status or security stamps, so do not claim that password/security-stamp updates invalidate existing sessions without a correction that enforces it.

Refresh tokens are stored raw in `IdentityContext`. Successful refresh rotates a valid token; reuse of a revoked token revokes active tokens for that user; logout treats missing, invalid, expired, or revoked supplied tokens as a non-failing path. Cookie paths differ across login, refresh, 2FA, and logout, so inspect the entire flow before changing it.

API keys arrive in `X-API-Key`, are compared as SHA-256 hashes, and are eligible only when key and client status permit. Failures intentionally conceal the reason. Raw values are returned only during creation and must never be persisted or logged.

Configured policies are not necessarily applied. Country permission policies exist but are not attached to Country actions; API-client administration lacks explicit authorization; and audit queries require authentication but lack a dedicated role/permission policy. API-client `RateLimitPerMinute` is stored but not enforced. The public initial-password endpoint also needs explicit security review. These are risks, not patterns to reproduce.

Documentation and logs may name configuration keys but must never include secret values.
