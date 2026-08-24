# Endpoint Development Flow

All implementation work requires an approved blueprint before files are changed.

```text
Requirement
  -> identify module and equivalent endpoint
  -> evaluate entity and EF configuration
  -> evaluate ViewModel/request/response
  -> evaluate repository
  -> explicitly decide whether a service is required
  -> command or query + handler
  -> controller endpoint
  -> tests
  -> focused build/test, then solution verification
```

## 1. Domain entity

Required for new persisted concepts or approved changes to existing models. Domain owns Country, API-client, and audit entities; Identity owns `ApplicationUser` and `RefreshToken`. Inspect base classes, nullability, keys, audit/status fields, and navigation properties. Skip when the endpoint projects existing data or performs a workflow without a new model.

Examples: `domain/Models/Entities/Country.cs`, `identity/Models/Entities/ApplicationUser.cs`.

## 2. EF configuration

Required when application persistence changes. Infrastructure uses `IEntityTypeConfiguration<T>` discovered from its assembly. Identity configures entities in `IdentityContext.OnModelCreating`. Define lengths, required fields, indexes, relationships, delete behavior, conversions, and defaults explicitly where neighboring entities do.

Examples: `CountryConfiguration.cs`, `ApiClientConfiguration.cs`, `IdentityContext.cs`.

## 3. ViewModel or DTO

Use the type placement already established by the module. Public feature ViewModels usually live in Domain. Identity has internal DTOs that handlers map to Domain response ViewModels. Skip a new type when an existing contract accurately represents the endpoint.

## 4. Repository

Country and audit reads use Application repository contracts with Infrastructure implementations. Extend an existing repository when it owns the entity; do not create a repository only to wrap a single existing operation. Read queries default to no tracking; mutations load tracked entities.

## 5. Service decision

Every endpoint blueprint must state `Service required: Yes` or `No`.

Use a service when coordinating Identity APIs, multiple entities/repositories, external email/token/key work, reusable business workflows, or transactions. Direct repository use is appropriate for a small single-aggregate operation such as Country CRUD. API-client direct-controller service use exists but is not automatically the default.

## 6. Command/query and handler

Use the custom mediator types in `application/Shared/Mediator`. Commands change state; queries read. Handlers commonly share a file with their request. Return the result shape used by the module, propagate cancellation, and enforce rules at the established layer.

## 7. Controller and endpoint

Inspect the neighboring controller for its route, base class, authorization, rate limit, response envelope, ProblemDetails behavior, and Swagger annotations. Keep business logic out of controllers. Explicitly verify permissions; authentication alone is not permission enforcement.

## 8. Tests and verification

Add tests proportional to the rule and failure modes. Run the affected test project first, then the solution build/test commands. Database migration generation and application are separate approval-gated operations.
