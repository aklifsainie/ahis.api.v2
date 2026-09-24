# Account Module

## Ownership and flow

Account owns registration, email confirmation, initial and changed passwords, profile, current-account view, and authenticator setup. `AccountController` dispatches custom mediator requests; handlers coordinate with `IAccountService`; the service uses ASP.NET Core Identity managers, `IdentityContext`, and SMTP where needed.

## Observed behavior

- Registration creates an active, non-deleted user without a password and attempts confirmation email delivery; email delivery is not transactional with user creation.
- Valid confirmation sets Identity email confirmation and `EmailVerifiedAt` in UTC, then sends a 30-minute, purpose-bound initial-password setup link to the configured public client URL. Setup validates the current security stamp, confirmed active account state, and absence of a password.
- Profile update requests mark the account configured. Authenticator setup is available only before 2FA is enabled; it resets a key without enabling 2FA. ASP.NET Core Identity rotates the security stamp during that reset, so the bearer used for setup no longer validates afterward. Successful enable verifies a code, enables 2FA, and creates recovery codes.
- Password changes and authenticator disable now rotate the Identity security stamp and revoke refresh tokens; bearer validation checks a stamp-derived version.
- Re-authentication returns a five-minute proof bound to the current user and security version. MFA reset, confirmed email change, deactivation, revoke-all, and revocation of another session require that proof. `POST /api/account/sessions/revoke-all` invalidates every refresh session, including the caller's, rotates the security version, clears the refresh cookie after commit, and requires a fresh login.
- `GET /api/account/sessions` returns a paginated, owner-only projection of active sessions using opaque public IDs and UTC lifecycle times. `DELETE /api/account/sessions/{sessionId}` is idempotent, requires a step-up proof for another session, and clears the refresh cookie only when it ends the current session. Bearer validation requires an active session public ID, so an ended session's access token is immediately rejected; older tokens without that claim require a new login. Device, IP, and location metadata are not collected.
- `GET /api/account/security-summary` returns only confirmation, credential-presence, MFA, recovery-code-count, and active-session-count state. It deliberately excludes credential material and timestamps whose source is not reliable.
- Email changes use Identity's change-email token and update the username only when it matched the previous email. Deactivation sets both inactive and soft-delete state; recovery has no self-service path.
- Password recovery starts through `POST /api/account/recovery/start` with a generic response and a verified-email challenge. Completion requires that challenge and, for MFA-enabled accounts, an authenticator or unused recovery code; it resets the password and invalidates sessions. Accounts that are deactivated or have lost all MFA factors require support intervention.
- `POST /api/account/2fa/recovery-codes/regenerate` requires a five-minute step-up proof and enabled MFA. It replaces all existing Identity-store recovery codes, returns the new codes only in the successful response, sends a best-effort security notification, and does not revoke sessions or rotate the security stamp.

## Risks and evidence boundaries

The initial password setup email requires `Identity:PublicClientBaseUrl` in deployment configuration. Existing registration callbacks are still client supplied and form a security trust boundary. The Identity migration that removes the legacy plaintext recovery-code column must be applied through the approved deployment process.

These are source-derived observations, not owner-confirmed product requirements. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).
