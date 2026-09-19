# Authentication Business Rules

## AUT-001 — Login eligibility

Users that are inactive or soft-deleted cannot log in. Identity also enforces configured confirmation and lockout settings. Confidence: Confirmed.

## AUT-002 — Lockout on failed login

Password login calls Identity with `lockoutOnFailure=true`; configured development settings permit three failed attempts before a five-minute lockout. Confidence: Confirmed for the current configuration.

## AUT-003 — Two-factor completion

When Identity requires 2FA, password login returns a response indicating 2FA is required without issuing tokens. Authenticator and recovery-code providers are supported. Confidence: Confirmed. The handler's current-user requirement conflicts with this intended flow.

## AUT-004 — Refresh-token rotation

A valid, unexpired refresh token is revoked and replaced during refresh. Confidence: Confirmed.

## AUT-005 — Reuse response

Use of a revoked refresh token causes all active refresh tokens for the user to be revoked. Confidence: Confirmed.

## AUT-006 — Idempotent logout

Missing, invalid, expired, or already revoked refresh tokens do not make logout fail. Confidence: Confirmed.

## AUT-007 — Password reset session invalidation

Successful password reset updates the Identity security stamp. Confidence: Confirmed.

## AUT-008 — Account-state enumeration resistance

Unknown accounts receive a successful generic negative state from account-state checking. Confidence: Confirmed. Forgot-password currently returns an error for unknown/unconfirmed users despite comments stating enumeration resistance.
