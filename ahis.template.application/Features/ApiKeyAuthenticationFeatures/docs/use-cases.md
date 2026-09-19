# API Client Authentication Use Cases

- Create an API client, initial key, and permission set.
- Create an additional key for rotation.
- Revoke a client key with a reason.
- Deactivate a client and revoke all active keys.
- Authenticate one `X-API-Key` header.
- Emit client identity and permission claims.
- Update key last-used timestamp after successful authentication.

There are currently no list/detail/update/reactivation endpoints for API clients and no tests for this module.
