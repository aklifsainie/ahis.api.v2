# Solution Overview

AHIS API Template is a .NET 8 Web API template for account configuration, JWT/refresh-token authentication, authenticator 2FA, external API-client keys, Country reference data, auditing, and human role/permission administration.

It uses layers plus feature folders, not a strict textbook architecture. The custom mediator dispatches most Application requests and invokes registered FluentValidation validators. Persistence and orchestration vary by module:

- Country uses Application repository contracts, Infrastructure repositories, and `IUnitOfWork`.
- Account and Authentication handlers delegate to Identity services.
- Identity administration handlers delegate role, permission, bootstrap, and session-invalidation work to Identity services.
- API-client administration calls an Infrastructure service from its controller.
- Audit queries execute EF operations in Application over an Infrastructure-provided `IQueryable`; automatic and explicit audit writes use different guarantees.

The two SQL Server contexts use the same configuration key but own separate migrations: `ApplicationDbContext` owns Country, API-client, and audit data; `IdentityContext` owns ASP.NET Core Identity and refresh tokens. See [project map](project-map.md), [dependency rules](dependency-rules.md), and [request flows](request-lifecycle.md).

The code, project references, migrations, tests, and runtime configuration outrank this document. Module descriptions distinguish observed implementation behavior from owner-confirmed requirements.
