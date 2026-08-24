# Authentication Module Overview

Authentication establishes and renews user sessions through ASP.NET Core Identity, JWT access tokens, and database-backed refresh tokens. Application handlers delegate to `AuthenticationService`.

```text
AuthenticationController
  -> custom mediator
  -> Authentication command/query handler
  -> IAuthenticationService
  -> UserManager / SignInManager / IdentityContext
```

Access tokens are returned in response bodies. Refresh tokens are normally stored in HttpOnly secure cookies and persisted in `RefreshTokens`.
