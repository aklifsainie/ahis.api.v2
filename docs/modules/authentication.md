# Authentication Module

## Ownership and flow

Authentication owns password login, 2FA completion, JWT issuance, refresh rotation, logout, and password reset. The controller dispatches through the custom mediator; handlers delegate multi-step work to `IAuthenticationService`, Identity managers, `IdentityContext`, and SMTP.

## Observed behavior

- Password login rejects inactive/deleted users and uses `lockoutOnFailure`; the observed development configuration is three attempts and five minutes. A two-factor-required login creates a five-minute protected challenge bound to the user's current security version; it is an `HttpOnly`, `Secure`, `SameSite=Lax` cookie scoped to `/api`, and completion derives the user only from that challenge.
- Successful refresh rotates a hash-addressed token in its random-public-ID session and records a parent relationship. It requires the session to remain active before it stores the replacement token. Revoked-token reuse bulk-revokes active tokens and sessions; logout revokes the current session and is non-failing for unusable supplied tokens.
- Password reset updates the Identity security stamp. Forgot-password and confirmation-resend responses return the same success result for every syntactically valid email.
- Bearer validation now rejects missing security-version or access-token-use claims and inactive, deleted, locked-out, or security-version-mismatched users. Refresh rotation applies the same checks; refresh records without a version are rejected. Step-up proofs use the same signing configuration but carry a distinct token-use claim and cannot authenticate API requests.

## Risks and exceptions

The `refresh_token` cookie is `Secure`, `HttpOnly`, `SameSite=Lax`, and scoped to `/api`; logout and security invalidation clear legacy paths during transition. Anonymous authentication and recovery actions use per-IP fixed-window limits; authenticated account-security actions use per-user fixed-window limits. The in-process limiter remains instance-local, so multi-instance deployments need a shared edge or distributed limit for account-wide abuse control. The security-version and recovery-code removal migrations require approved deployment application.

Treat these as observed risks, not requirements. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).
