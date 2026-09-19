# Account Module Overview

Account Management covers registration and post-registration user configuration. Application handlers are thin orchestration layers over `IAccountService`; `AccountService` uses ASP.NET Core Identity managers and SMTP email.

```text
AccountController
  -> custom mediator
  -> Account command/query handler
  -> IAccountService
  -> UserManager / SignInManager / email sender
  -> IdentityContext
```

Account and Authentication share `ApplicationUser` and Identity services but expose separate feature folders and controllers.
