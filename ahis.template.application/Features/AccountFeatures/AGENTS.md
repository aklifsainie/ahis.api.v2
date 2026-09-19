# Account Feature Instructions

## Purpose and ownership

Account owns user registration and configuration: email confirmation, initial/change password, profile, current-account view, and authenticator setup/enable/disable.

## Owned code

- Application: this folder
- Domain responses: `ahis.template.domain/Models/ViewModels/AccountVM`
- Identity contracts/implementation: `identity/Interfaces/IAccountService.cs`, `identity/Services/AccountService.cs`, Identity DTOs and `ApplicationUser`
- API: `api/Controllers/v1/AccountController.cs`

## Established flow

Controllers send custom mediator requests. Handlers obtain the current user where required and delegate multi-step Identity behavior to `IAccountService`. Keep ASP.NET Identity operations, confirmation/password/2FA workflows, and Identity persistence in the Identity service. Do not add an application repository for Identity users merely to match Country.

## Security and rules

- Registration creates a user without a password and sends confirmation email.
- Initial password setup is only for users without a password.
- Protected self-service operations derive user ID from the authenticated principal.
- Profile update marks the account configured.
- Enabling 2FA requires a valid code and returns recovery codes once; disabling clears authenticator material.
- Password change updates the security stamp.

## Before modifying

Read this folder's docs, the relevant command/query, `AccountController`, `IAccountService`, `AccountService`, `ApplicationUser`, relevant Identity DTOs, Identity configuration in `Program.cs`, and Authentication rules when tokens/sessions are affected. Produce a blueprint and wait for approval.

## Testing

There are no Account tests yet. Propose handler tests and, for Identity behavior, an appropriate integration strategy rather than assuming repository mocks.
