# Dependency Rules

## Observed graph

```text
API -> Application, Identity, Infrastructure
Infrastructure -> Application, Domain
Application -> Domain, Identity
Identity -> Domain
Tests -> Application, Domain
```

Domain has no project references. Application owns request orchestration, mediator abstractions, and application persistence contracts. Infrastructure implements application persistence and integration contracts. Identity owns both its service interfaces and implementations, and Application consumes those interfaces directly. API is the composition root.

This is not strict Clean Architecture. Do not characterize it that way or refactor it toward that model without an approved architectural change.

## Extension guidance

Choose the owning module and nearest working example before adding a layer. A simple Country-style aggregate generally follows Domain entity → Infrastructure configuration → Application repository contract → Infrastructure repository → Application request/handler → API controller. Identity workflows delegate from the handler to `IAccountService` or `IAuthenticationService`.

Two exceptions must remain visible: API-client administration uses a direct controller-to-service path, and the audit handler executes EF queries after a repository exposes `IQueryable`. Neither is the default for new work.
