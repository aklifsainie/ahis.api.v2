# Codex Operating Guide

## System overview

This .NET 8 Web API template provides account management, JWT and refresh-token authentication, authenticator-based 2FA, API-client/API-key authentication, country reference data, and audit logging. Existing source code is the authority. Preserve module-specific patterns and do not remodel the solution toward a preferred architecture.

## Technology stack

- .NET 8 and ASP.NET Core Web API
- ASP.NET Core Identity with JWT bearer authentication
- EF Core with SQL Server (EF Core 9 in API/Infrastructure; EF Core 8 in Identity)
- Custom `IMediator`/`IRequest`/`IRequestHandler` implementation; this repository does not use MediatR
- FluentResults and FluentValidation
- xUnit, Moq, FluentAssertions, and coverlet
- Swagger/OpenAPI and ASP.NET Core rate limiting

## Solution map

- `ahis.template.api`: composition root, controllers, authentication handlers, authorization, Swagger, and rate limiting.
- `ahis.template.application`: feature commands/queries/handlers, repository and service contracts, custom mediator, validation, results, and application services.
- `ahis.template.domain`: shared entities, view models, enums, base entities, and `IUnitOfWork`.
- `ahis.template.infrastructure`: `ApplicationDbContext`, EF configurations/migrations, repositories, audit persistence, and API-key implementations.
- `ahis.template.identity`: ASP.NET Core Identity entities/services, `IdentityContext`, refresh-token persistence, and Identity migrations.
- `ahis.template.test`: xUnit tests; currently focused on Country query handlers.

## Dependency rules

The current dependency graph is intentional context, even where it differs from strict Clean Architecture:

```text
API -> Application, Identity, Infrastructure
Infrastructure -> Application, Domain
Application -> Domain, Identity
Identity -> Domain
Tests -> Application, Domain
```

- Keep repository contracts in Application and application repository implementations in Infrastructure.
- Keep Identity-specific EF/Identity behavior in the Identity project unless an existing feature demonstrates otherwise.
- Controllers normally delegate to the custom mediator. API-client administration is an existing direct-service variation; inspect the affected module before choosing it.
- Do not introduce MediatR or a new architectural framework without an approved architectural change.

See `docs/architecture/dependency-rules.md` for detail.

## Module map

- Account Management: `ahis.template.application/Features/AccountFeatures`; read its `AGENTS.md`.
- Authentication: `ahis.template.application/Features/AuthenticationFeatures`; read its `AGENTS.md`.
- API Client Authentication: `ahis.template.application/Features/ApiKeyAuthenticationFeatures`; read its `AGENTS.md`.
- Country Reference Data: `ahis.template.application/Features/CountryFeatures`; read its `AGENTS.md`.
- Audit logging is a cross-cutting concern documented in `docs/architecture/auditing.md`.

## Default endpoint development workflow

Evaluate each step and skip it only when it does not apply:

1. Create or modify the Domain entity.
2. Create or modify the Infrastructure `IEntityTypeConfiguration<T>`.
3. Create or modify the Domain ViewModel/response model when that matches the module.
4. Create or extend the Application repository contract.
5. Create or extend the Infrastructure repository implementation.
6. Explicitly decide whether a service is necessary.
7. Add the Application command/query and handler using the custom mediator.
8. Create or modify the API controller.
9. Add the endpoint with its route, authorization, result mapping, and cancellation behavior.
10. Add or update tests.
11. Run focused verification, then broader build/tests.

Country is the primary repository-based example. Account and Authentication are service-based Identity examples. See `docs/architecture/endpoint-development-flow.md`.

## Mandatory architectural approval gate

Before creating or modifying implementation code:

1. Read this file and every applicable nested `AGENTS.md`.
2. Inspect an equivalent implementation and trace its dependencies.
3. Produce an architectural blueprint naming the module, flow, service decision, business rules, database/API impact, tests, and exact files to add or modify.
4. Wait for explicit user approval.
5. Implement only after approval.

The gate applies to implementation, schema, infrastructure, repository/service, business-rule, endpoint, module, and refactoring changes. It does not block read-only explanation, tracing, review, or diagnosis. If diagnosis leads to a proposed code fix, present the blueprint before editing.

## Coding conventions

- Use file-scoped behavior only when the surrounding module does; current code predominantly uses block namespaces.
- Commands mutate state; queries read state. Commands/queries and handlers are commonly colocated in one file.
- Requests implement the repository's custom `IRequest<TResponse>` and handlers implement its `IRequestHandler<TRequest,TResponse>`.
- Use `FluentResults.Result`/`Result<T>` and the established typed errors where the module uses them.
- Propagate `CancellationToken` through controller, mediator, repository, EF, and audit calls when APIs support it.
- Use no-tracking queries for reads and tracked entities for updates.
- `BaseEntity` records use `IsDelete` for soft deletion. Active lookup reads commonly require both non-deleted and `IsActive`.
- Persist application changes through `IUnitOfWork` when following the Country/repository pattern.
- Keep ViewModels in Domain where existing features do so; do not relocate them based on generic guidance.
- Use UTC for persisted timestamps.
- Do not log raw tokens, passwords, API keys, SMTP credentials, JWT signing keys, or other secrets.
- Preserve current variations in routes, validation, and response mapping unless a separately approved refactor addresses them.

## Build and test

From the solution root:

```powershell
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

Prefer the affected project and affected tests first. Never run `dotnet ef database update` or apply a migration to a database unless explicitly requested. Migration generation also requires an approved migration blueprint.

## Documentation map

- Architecture overview: `docs/architecture/solution-overview.md`
- Projects and dependencies: `docs/architecture/project-map.md`, `dependency-rules.md`
- Endpoint workflow and runtime traces: `endpoint-development-flow.md`, `request-lifecycle.md`
- EF Core and migrations: `persistence.md`
- Results and errors: `error-handling.md`
- Security: `authentication-authorization.md`
- Auditing: `auditing.md`
- Tests: `testing-strategy.md`
- Terms: `docs/glossary/business-terms.md`

Update only documentation made inaccurate by an approved change. Mark inferred behavior as inferred rather than confirmed.
