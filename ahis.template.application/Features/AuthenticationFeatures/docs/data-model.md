# Authentication Data Model

Authentication uses `ApplicationUser` from Identity and `RefreshToken`:

- `RefreshToken.Id`: database-generated integer key
- `UserId`: Identity user identifier; indexed but not configured as a foreign key
- `Token`: raw persisted refresh token, maximum length 450
- `ExpiresAt`, `CreatedAt`, `IsRevoked`, and optional `RevokedAt`

JWTs include name identifier, username, email, subject, unique-name, JWT ID, custom user claims, and roles. Expiry, issuer, audience, and signing key come from configuration.
