# Authentication Feature Instructions

## Purpose and ownership

Authentication owns account-state discovery, password login, 2FA login completion, JWT issuance, refresh-token rotation, logout, forgot/reset password, and token diagnostic endpoints.

## Owned code

- Application: this folder
- Domain contracts: `domain/Models/ViewModels/AuthenticationVM`, `domain/Enums/TwoFactorProviderEnum.cs`
- Identity: `identity/Interfaces/IAuthenticationService.cs`, `identity/Services/AuthenticationService.cs`, `IdentityContext`, `RefreshToken`
- API: `api/Controllers/v1/AuthenticationController.cs`

## Established flow

Controllers send requests through the custom mediator. Handlers delegate multi-step work to `IAuthenticationService`. That service coordinates Identity managers, JWT creation, refresh-token persistence, email, and transactions. Preserve this service boundary for authentication workflows.

## Security rules

- Never log credentials, raw access/refresh tokens, reset tokens, signing keys, or authenticator/recovery codes.
- Inactive/deleted users cannot log in.
- Login failures participate in Identity lockout.
- Refresh is rotating; reuse revokes active sessions.
- Logout is idempotent.
- Preserve generic public responses where enumeration resistance is intended, and flag existing contradictions rather than copying them.

## Before modifying

Read this folder's docs, the relevant controller action/handler/service method, `ApplicationUser`, `RefreshToken`, `IdentityContext`, JWT/Identity setup in `Program.cs`, and Account docs if password/email/2FA setup is affected. Produce a blueprint and wait for approval.

## Known flow risk

The current 2FA verification handler reads the authenticated current user even though login does not issue a token when 2FA is required. Treat correction as a separate approved behavior/security change.
