# Project Map

| Project | Type | Responsibility | References | Persistence/API/tests |
|---|---|---|---|---|
| `ahis.template.api` | ASP.NET Core Web SDK | Composition root, controllers, JWT/API-key handlers, policies, rate limiting, Swagger | Application, Identity, Infrastructure | Executable API |
| `ahis.template.application` | Class library | Commands, queries, handlers, contracts, custom mediator, results, application services | Domain, Identity | Application logic |
| `ahis.template.domain` | Class library | Shared entities, ViewModels, enums, base models, unit-of-work contract | None | Domain models |
| `ahis.template.infrastructure` | Class library | Application EF context/configurations/migrations, repositories, API-key and audit implementations | Application, Domain | SQL Server persistence |
| `ahis.template.identity` | Class library | Identity entities, account/authentication services, Identity context and migrations | Domain | Identity persistence |
| `ahis.template.test` | Test project | Handler and Identity token-state unit tests | Application, Domain, Identity | xUnit tests |

## Package observations

- Every project targets `net8.0` with nullable reference types enabled.
- API and Infrastructure reference EF Core 9.0.7; Identity references EF Core 8.0.22.
- Application references FluentResults 4.0.0; Identity references FluentResults 3.11.0.
- No central package management, `global.json`, `Directory.Build.*`, Docker files, worker projects, or CI workflow were found at bootstrap time.

These version differences are existing constraints, not permission to normalize packages during feature work.
