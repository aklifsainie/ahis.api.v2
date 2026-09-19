# Account Business Rules

## ACC-001 — Registration starts without a password

Registration creates an active, non-deleted user with unconfirmed email and sends a confirmation link. Confidence: Confirmed.

## ACC-002 — Email confirmation records verification time

Valid Identity confirmation changes `EmailConfirmed` and sets `EmailVerifiedAt` to UTC. Confidence: Confirmed.

## ACC-003 — Initial password is single-purpose

Initial password setup fails if the user already has a password; the change-password flow must be used. Confidence: Confirmed.

## ACC-004 — Profile completion

The current update-profile handler sets `MarkAccountConfigured=true`, causing `IsAccountConfigured` to become true. Confidence: Confirmed.

## ACC-005 — Authenticator activation

Generating setup resets the authenticator key but does not enable 2FA. Enabling requires a valid authenticator code, enables 2FA, records UTC activation time, and generates ten recovery codes. Confidence: Confirmed.

## ACC-006 — Authenticator deactivation

Disabling 2FA clears the key, URI, recovery codes, and activation timestamp. Confidence: Confirmed.

## ACC-007 — Password change invalidates sessions

Successful password change updates the Identity security stamp. Confidence: Confirmed.
