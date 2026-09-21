# AHIS API Template — Codex Guide

## Scope and source of truth

This is a .NET 8 ASP.NET Core Web API template for accounts, authentication, API-client keys, Country reference data, and auditing. Source code, project references, migrations, tests, and runtime configuration are authoritative; this guide routes work to the relevant evidence. Do not redesign the existing layered, feature-folder structure during unrelated work.

## Solution map and dependencies

- `ahis.template.api`: executable composition root, controllers, authentication, authorization, rate limiting, and Swagger.
- `ahis.template.application`: feature requests/handlers, contracts, custom mediator, validation, and results.
- `ahis.template.domain`: shared entities, view models, enums, and `IUnitOfWork`.
- `ahis.template.infrastructure`: application EF Core context, migrations, repositories, API-key implementation, and auditing.
- `ahis.template.identity`: ASP.NET Core Identity entities, services, context, and migrations.
- `ahis.template.test`: xUnit handler unit tests.

The actual graph is `API -> Application, Identity, Infrastructure`; `Infrastructure -> Application, Domain`; `Application -> Domain, Identity`; `Identity -> Domain`; and `Tests -> Application, Domain`. This is not strict Clean Architecture: Application consumes Identity contracts. See [project map](docs/architecture/project-map.md) and [dependency rules](docs/architecture/dependency-rules.md).

## Modules and local guidance

- [Account](docs/modules/account.md): `ahis.template.application/Features/AccountFeatures`; read its `AGENTS.md`.
- [Authentication](docs/modules/authentication.md): `ahis.template.application/Features/AuthenticationFeatures`; read its `AGENTS.md`.
- [API-client authentication](docs/modules/api-client-authentication.md): `ahis.template.application/Features/ApiKeyAuthenticationFeatures`; read its `AGENTS.md`.
- [Country](docs/modules/country.md): `ahis.template.application/Features/CountryFeatures`; read its `AGENTS.md`.
- [Audit](docs/modules/audit.md): `ahis.template.application/Features/AuditLogFeatures`; read its `AGENTS.md` when that module is affected.

Use Country as the simple repository/unit-of-work pattern, Account and Authentication as Identity-service patterns, and API-client administration only as its documented direct-service variation. The audit query's `IQueryable`/EF execution in Application is an exception, not a default.

## Change planning and approval

Use the relevant repository skill in `.agents/skills/`. The repository uses a risk-based blueprint gate:

- Obtain explicit approval before changes to architecture, module boundaries, public APIs, business behavior, authorization or security, database schema or migrations, dependencies, external integrations, production configuration, or broad refactors.
- Small local fixes, tests, and documentation changes may proceed when they are within the active authorized request and do not cross a risk boundary.
- Stop and revise the blueprint when discovery reveals a material scope, contract, schema, security, dependency, or behavior change.
- Never apply a migration, deploy, publish, push, or mutate an external system without separate explicit authorization.

## Durable implementation and review rules

- Use the repository's custom `IMediator`, `IRequest<TResponse>`, and `IRequestHandler<TRequest,TResponse>`—not MediatR.
- Choose ownership from the closest module example. Keep application persistence contracts in Application and their implementations in Infrastructure; retain Identity workflows in the Identity service boundary.
- Propagate cancellation where surrounding APIs support it. Use no-tracking reads and tracked mutations when following repository patterns.
- Treat `BaseEntity.IsDelete` as soft deletion. Do not assume a configured policy is enforced: inspect the controller/action.
- Never log or document passwords, raw access/refresh tokens, raw API keys outside their one-time creation response, signing keys, SMTP credentials, or unmasked sensitive audit values.
- Treat implementation findings as observations unless an owner has confirmed them as requirements. Preserve known risks rather than copying them as conventions.

Read [endpoint flow](docs/architecture/endpoint-development-flow.md), [request flows](docs/architecture/request-lifecycle.md), [persistence](docs/architecture/persistence.md), [security](docs/architecture/authentication-authorization.md), [auditing](docs/architecture/auditing.md), [errors](docs/architecture/error-handling.md), and [review guidance](REVIEW.md) as applicable.

## Validation

Run verification narrow to broad. The usual commands from the repository root are:

```powershell
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

Restore, build, and test can create caches and build outputs; report their results accurately. Never use `dotnet ef database update` as routine verification. See [testing strategy](docs/architecture/testing-strategy.md).

Refresh this guidance when the dependency graph, a module boundary, persistence/security behavior, or the Codex operating model changes; otherwise review by 2027-03-21.
