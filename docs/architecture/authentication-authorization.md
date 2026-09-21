# Authentication and Authorization

JWT validation checks issuer, audience, lifetime, signing key, an access-token-use claim, user existence, active/deleted state, lockout, and a SecurityStamp-derived version. Password login rejects inactive/deleted users and uses Identity lockout handling. Refresh rotation checks the same account state and stored security version. Legacy tokens without a version or access-token-use claim fail closed; deployment requires the planned Identity migration.

Refresh tokens are stored in `IdentityContext` only as SHA-256 hashes. Each completed login has a random public-ID session; rotations add a child token linked to the presented parent, update session activity, and use a conditional revocation update so only one concurrent rotation can succeed. Reuse of a revoked token invalidates active tokens and sessions for that user. Logout revokes the current session and remains non-failing for missing, invalid, expired, or revoked supplied tokens. A daily hosted cleanup removes up to 500 leaf token rows retained for at least seven days after expiry, then up to 500 ended sessions retained for at least 30 days after expiry or revocation. Cookie paths differ across login, refresh, 2FA, and logout, so inspect the entire flow before changing it.

API keys arrive in `X-API-Key`, are compared as SHA-256 hashes, and are eligible only when key and client status permit. Failures intentionally conceal the reason. Raw values are returned only during creation and must never be persisted or logged.

Configured policies are not necessarily applied. Country permission policies exist but are not attached to Country actions; API-client administration lacks explicit authorization; and audit queries require authentication but lack a dedicated role/permission policy. API-client `RateLimitPerMinute` is stored but not enforced. The public initial-password endpoint also needs explicit security review. These are risks, not patterns to reproduce.

Documentation and logs may name configuration keys but must never include secret values.

## Planned security work

Proposed Identity security hardening and endpoints are tracked in the [Identity security endpoint backlog](../backlog/identity-security-endpoints.md). That checklist records current prerequisites, dependencies, implementation status, and evidence; it is not an approved implementation blueprint.
