# Dependency Rules

## Actual graph

```text
ahis.template.api
  -> ahis.template.application
  -> ahis.template.identity
  -> ahis.template.infrastructure

ahis.template.infrastructure
  -> ahis.template.application
  -> ahis.template.domain

ahis.template.application
  -> ahis.template.domain
  -> ahis.template.identity

ahis.template.identity
  -> ahis.template.domain

ahis.template.test
  -> ahis.template.application
  -> ahis.template.domain
```

## Placement rules supported by source

- Domain has no project references and holds shared non-Identity entities and ViewModels.
- Application owns orchestration requests/handlers and contracts for application repositories/services.
- Infrastructure implements application persistence and integration contracts.
- Identity owns its interfaces and implementations together; Application currently consumes those interfaces directly.
- API is the composition root and may reference all runtime projects.
- Tests currently exercise Application handlers through mocks.

This is not strict Clean Architecture because Application depends on Identity. Do not describe or refactor it as strict Clean Architecture without an approved architectural change.

## Extension rule

Choose the owning module first, then copy its nearest equivalent. A new Country-style entity normally follows Domain entity -> Infrastructure configuration -> Application repository contract -> Infrastructure repository -> Application request/handler -> API controller. An Identity workflow normally delegates from its handler to `IAccountService` or `IAuthenticationService`.
