# Account Module

## Ownership and flow

Account owns registration, email confirmation, initial and changed passwords, profile, current-account view, and authenticator setup. `AccountController` dispatches custom mediator requests; handlers coordinate with `IAccountService`; the service uses ASP.NET Core Identity managers, `IdentityContext`, and SMTP where needed.

## Observed behavior

- Registration creates an active, non-deleted user without a password and attempts confirmation email delivery; email delivery is not transactional with user creation.
- Valid confirmation sets Identity email confirmation and `EmailVerifiedAt` in UTC. Initial-password setup refuses an account that already has a password.
- Profile update requests mark the account configured. Authenticator setup is available only before 2FA is enabled; it resets a key without enabling 2FA. ASP.NET Core Identity rotates the security stamp during that reset, so the bearer used for setup no longer validates afterward. Successful enable verifies a code, enables 2FA, and creates recovery codes.
- Password changes and authenticator disable now rotate the Identity security stamp and revoke refresh tokens; bearer validation checks a stamp-derived version.

## Risks and evidence boundaries

`set-password` is public and receives a user ID without observed caller binding. Disabling 2FA clears custom user fields but does not demonstrably reset all Identity token-store material. Repeated enable operations can issue fresh recovery codes. Callback base URLs are client supplied and form a security trust boundary.

These are source-derived observations, not owner-confirmed product requirements. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).
