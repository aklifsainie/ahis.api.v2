# P1 blueprint: View active sessions

Status: Implemented (2026-09-22).

`GET /api/account/sessions` uses the Account controller, custom mediator, and `IAccountService` to return a paginated projection of the caller's active `RefreshSession` rows. It accepts no user selector, requires bearer authentication and `AuthenticatedSecurityPolicy`, and audits successful reads without sensitive values.

The response exposes only the opaque session public ID, UTC creation/last-used/expiry timestamps, and `isCurrent`. New access tokens include the public session ID claim; tokens issued before this endpoint have no current-session marker until refresh or a new login. Refresh-token values and hashes are never projected. No schema migration was required. Device, IP, and location metadata remain deferred pending privacy and retention policy approval.

Focused handler tests cover authenticated-owner routing, pagination bounds, response mapping, and audit routing. Relational query behavior and endpoint authorization require an environment-backed integration test if later assurance beyond unit coverage is needed.
