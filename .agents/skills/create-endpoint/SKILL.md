---
name: create-endpoint
description: Analyze and, after explicit blueprint approval, implement an API endpoint using the owning module's entity, repository or service, custom CQRS, controller, security, and test patterns.
---

# Create Endpoint

Read root and owning-module `AGENTS.md`, module docs, and `docs/architecture/endpoint-development-flow.md`. Trace the closest endpoint end to end.

Produce an **Endpoint Architectural Blueprint** before editing:

- Feature, module, and closest endpoint
- Runtime flow using the repository's custom mediator where applicable
- Entity: Create / Modify / Reuse / Not required
- EntityTypeConfiguration: Create / Modify / Not required
- ViewModel/DTO: Create / Modify / Reuse / Not required
- Repository: Create / Extend / Reuse / Not required
- Service required: Yes / No, with a concrete reason
- Command or query and handler
- Controller, HTTP verb, route, request/response, validation, status codes, authorization/policies, rate limiting, and cancellation
- Files to add and modify
- Business rules and confidence
- Database, migration, API, test, documentation, and security impact
- Risks and uncertainties

Stop and wait for explicit approval. After approval, implement only the blueprint, add focused tests, update affected docs, and run focused then solution verification. Migration generation/application remains separately gated.
