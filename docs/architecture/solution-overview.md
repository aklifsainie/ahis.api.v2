# Solution Overview

## Purpose

The solution is a production-oriented .NET 8 Web API template centered on account and authentication capabilities. It includes account registration and configuration, JWT/refresh-token authentication, authenticator 2FA, external API-client keys, Country reference data, and audit logging.

## Architectural shape

The solution uses layers plus feature folders rather than a strict textbook architecture. Application features use CQRS-style requests and handlers dispatched by a repository-owned custom mediator. Persistence differs by area:

- Country uses Application repository contracts, Infrastructure repositories, and `IUnitOfWork`.
- Account and Authentication handlers delegate to services in the Identity project.
- API-client administration currently calls an Infrastructure service directly from its controller.
- Audit querying uses a repository; audit writing uses an interceptor or explicit audit service.

Do not force these variations into one style during unrelated work. Select the closest established feature in the owning module and explain any proposed departure in the approval blueprint.

## Runtime components

- ASP.NET Core controllers expose HTTP endpoints.
- JWT bearer and API-key handlers create authenticated principals.
- The custom mediator locates request handlers and runs registered FluentValidation validators.
- `ApplicationDbContext` persists Country, API-client, and audit data.
- `IdentityContext` persists ASP.NET Core Identity data and refresh tokens.
- Both contexts are configured against `ConnectionStrings:DefaultConnection`.

## Source of truth

Source code, project references, EF configurations, migrations, tests, and runtime configuration outrank these documents. Update the documentation when an approved architectural or business change makes it inaccurate.
