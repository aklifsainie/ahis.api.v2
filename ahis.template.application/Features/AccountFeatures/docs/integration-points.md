# Account Integration Points

- `AccountController`: public registration/confirmation/initial-password actions and protected self-service actions.
- Custom mediator and Account handlers.
- `IAccountService`/`AccountService`.
- ASP.NET Core `UserManager` and `SignInManager`.
- `IEmailSender` SMTP implementation for confirmation messages.
- `ICurrentUserService` or, in some handlers, direct `IHttpContextAccessor` access.
- Authentication module when password/security-stamp changes affect sessions.

Callback base URLs currently come from requests; any trust/allow-list change is a security-sensitive architectural change.
