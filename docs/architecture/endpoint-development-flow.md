# Endpoint Development Flow

Use the closest active module flow, then decide what is actually needed:

```text
Requirement -> ownership and analogous endpoint -> entity/configuration when needed
-> request/response -> repository or service decision -> handler when applicable
-> controller/action -> tests -> focused-to-broad verification
```

For persisted application data, inspect Domain ownership, Infrastructure EF configuration, repository boundary, `IUnitOfWork`, audit implications, migration context, and API mapping. Identity users and refresh tokens remain in the Identity boundary. Public view models usually live in Domain, while Identity also uses internal DTOs.

Every material endpoint plan states whether a service is required and cites the analogous flow. Services fit Identity operations, multi-owner workflows, external email/token/key work, transactions, or reusable coordination; direct repository use fits simple single-aggregate work such as Country CRUD. The API-client direct-service pattern is local variation, not a template.

Check actual authorization, rate limits, result mapping, validation, cancellation, and sensitive-data handling. Architecture, behavior, public API, security, authorization, schema, migration, dependency, integration, production-configuration, and broad-refactor changes need explicit blueprint approval; see `.agents/skills/change-blueprint`.
