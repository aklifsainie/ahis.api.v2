# Account Module

## Ownership and flow

Account owns registration, email confirmation, initial and changed passwords, profile, current-account view, and authenticator setup. `AccountController` dispatches custom mediator requests; handlers coordinate with `IAccountService`; the service uses ASP.NET Core Identity managers, `IdentityContext`, and SMTP where needed.

## Observed behavior

- Registration creates an active, non-deleted user without a password and attempts confirmation email delivery; email delivery is not transactional with user creation.
- Valid confirmation sets Identity email confirmation and `EmailVerifiedAt` in UTC, then sends a 30-minute, purpose-bound initial-password setup link to the configured public client URL. Setup validates the current security stamp, confirmed active account state, and absence of a password.
- Profile update requests mark the account configured. Authenticator setup is available only before 2FA is enabled; it resets a key without enabling 2FA. ASP.NET Core Identity rotates the security stamp during that reset, so the bearer used for setup no longer validates afterward. Successful enable verifies a code, enables 2FA, and creates recovery codes.
- Password changes and authenticator disable now rotate the Identity security stamp and revoke refresh tokens; bearer validation checks a stamp-derived version.
- Re-authentication returns a five-minute proof bound to the current user and security version. MFA reset, confirmed email change, deactivation, and session revocation require that proof. `POST /api/account/sessions/revoke-all` invalidates every refresh session, including the caller's, rotates the security version, clears the refresh cookie after commit, and requires a fresh login.
- `GET /api/account/sessions` returns a paginated, owner-only projection of active sessions using opaque public IDs and UTC lifecycle times. New access tokens carry their session public ID to identify the current row; access tokens issued before this claim was added show no current row until refreshed or replaced. Device, IP, and location metadata are not collected.
- Email changes use Identity's change-email token and update the username only when it matched the previous email. Deactivation sets both inactive and soft-delete state; recovery has no self-service path.

## Risks and evidence boundaries

The initial password setup email requires `Identity:PublicClientBaseUrl` in deployment configuration. Existing registration callbacks are still client supplied and form a security trust boundary. The Identity migration that removes the legacy plaintext recovery-code column must be applied through the approved deployment process.

These are source-derived observations, not owner-confirmed product requirements. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).
