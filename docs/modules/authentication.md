# Authentication Module

## Ownership and flow

Authentication owns account-state checks, password login, 2FA completion, JWT issuance, refresh rotation, logout, password reset, and diagnostics. The controller dispatches through the custom mediator; handlers delegate multi-step work to `IAuthenticationService`, Identity managers, `IdentityContext`, and SMTP.

## Observed behavior

- Password login rejects inactive/deleted users and uses `lockoutOnFailure`; the observed development configuration is three attempts and five minutes.
- Successful refresh rotates a hash-addressed token in its random-public-ID session and records a parent relationship. Revoked-token reuse bulk-revokes active tokens and sessions; logout revokes the current session and is non-failing for unusable supplied tokens.
- Password reset updates the Identity security stamp. Account-state checking gives an unknown account a generic negative state.
- Bearer validation now rejects missing security-version or access-token-use claims and inactive, deleted, locked-out, or security-version-mismatched users. Refresh rotation applies the same checks; refresh records without a version are rejected. Step-up proofs use the same signing configuration but carry a distinct token-use claim and cannot authenticate API requests.

## Risks and exceptions

The 2FA completion handler requires current-user context although password login returns no token when 2FA is required. Refresh-cookie paths vary among actions. Forgot-password and resend-confirmation behavior conflicts with some enumeration-resistance comments. The security-version refresh column requires an approved Identity migration before deployment.

Treat these as observed risks, not requirements. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).
