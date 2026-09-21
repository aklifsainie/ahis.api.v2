# Account Feature Instructions

Account owns registration and post-registration configuration: email confirmation, initial and changed passwords, profile, current-account view, and authenticator setup.

Controllers dispatch through the custom mediator; handlers delegate Identity-specific multi-step behavior to `IAccountService`. Keep `UserManager`, `SignInManager`, confirmation/password/2FA operations, Identity persistence, and SMTP coordination in that service boundary. Do not add an application repository merely to mirror Country.

Before changing this module, read the closest command/query, `AccountController`, `IAccountService`, `AccountService`, `ApplicationUser`, related Identity configuration, and [the Account module guide](../../../docs/modules/account.md). Inspect Authentication when a password, email, token, or 2FA flow is involved.

Important observed risks: the initial-password endpoint is public and accepts a user ID; disabling 2FA does not demonstrably clear all Identity token-store material. JWT and refresh validation now enforce a SecurityStamp-derived version; the Identity migration remains required before deployment. Treat any further correction as a security-sensitive blueprinted change.

There are no Account tests. Select the test layer deliberately; Identity-manager behavior may need integration coverage rather than repository mocks.
