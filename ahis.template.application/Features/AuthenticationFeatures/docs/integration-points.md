# Authentication Integration Points

- `AuthenticationController` and refresh-token cookies.
- Custom mediator and Authentication handlers.
- `IAuthenticationService`/`AuthenticationService`.
- ASP.NET Core Identity managers and token providers.
- `IdentityContext` and `IdentityUnitOfWork`.
- JWT configuration and bearer validation in `Program.cs`.
- SMTP email for password-reset messages.
- Account module for email confirmation, password creation/change, and 2FA setup.

Cookie paths and SameSite values currently vary between login, 2FA, refresh, and logout actions; inspect all related actions before changing them.
