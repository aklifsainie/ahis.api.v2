# API-client Authentication Feature Instructions

This module owns external API clients, API keys, permission claims, key validation, revocation, rotation, and client deactivation.

Its administration endpoints are a local variation: controllers call `IApiClientService` directly, even though request models implement custom mediator interfaces. The service uses `ApplicationDbContext` directly. Do not copy this pattern to other modules without evidence and an explicit service decision.

Before changing it, inspect the affected controller/service/validator, API-key entities and EF configurations, authentication handler, relevant policies in `Program.cs`, and [the module guide](../../../docs/modules/api-client-authentication.md).

Never persist or log a raw API key; return it only from its creation response. API-client administration currently lacks explicit authorization and mutation auditing, while the stored per-client rate limit is not enforced. Treat those as security/behavior risks requiring a blueprinted change, not as conventions.
