# API Client Authentication Feature Instructions

## Purpose and ownership

This feature owns external API clients, API keys, permission claims, key validation, key rotation/revocation, and client deactivation.

## Owned code

- Application request models: this folder
- Application contracts: `Interfaces/Services/IApiClientService.cs`, `Interfaces/Validators/IApiKeyValidator.cs`
- Domain: `Models/Entities/ApiKey`, `Models/ViewModels/ApiKeyAuthenticationVM`
- Infrastructure: `ApiClientAuthentication`, `Services/ApiClientService.cs`, EF configurations, `ApplicationDbContext`
- API: `ApiClientAuthentication` handler classes and `Controllers/v1/ApiClientController.cs`

## Established variation

Administration endpoints currently call `IApiClientService` directly; their request models implement custom mediator request interfaces but have no handlers. The service directly uses `ApplicationDbContext`. This is established code but not the default for unrelated modules. Any new endpoint blueprint must decide whether to preserve this variation or complete a CQRS flow.

## Security rules

- Return a raw API key only from its creation operation; persist only its hash and a non-secret prefix.
- Key use requires active/unrevoked/unexpired key plus active client.
- Do not reveal the specific reason an authentication key failed.
- Normalize permissions to lowercase and remove duplicates.
- Deactivating a client revokes active keys.

## Before modifying

Read this folder's docs, all API-key entities/configurations, `ApiClientService`, `ApiKeyValidator`, the API authentication handler, controller, policies in `Program.cs`, and migrations. Produce a blueprint and wait for approval.

## Known security risk

`ApiClientController` currently has no authorization attribute. Do not copy that omission. Authorization changes require an explicit security-impact blueprint and tests.
