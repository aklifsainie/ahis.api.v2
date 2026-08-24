# Repository Review Guide

Review changes against the existing module pattern and its applicable `AGENTS.md`. Prioritize correctness, security, behavior changes, and missing tests over style preferences.

## Architecture and ownership

- Reject wrong-direction project references not justified by the current dependency graph.
- Keep repository contracts in Application and application persistence implementations in Infrastructure.
- Flag direct `ApplicationDbContext` use when the affected module follows the repository pattern.
- Flag unnecessary services for single-repository operations, and overloaded handlers where a real multi-step workflow belongs in an established service.
- Flag business logic in controllers. Note that API-client administration is a known legacy variation, not a default template.
- Ensure new requests use the custom mediator abstractions, not MediatR types.

## API and security

- Verify explicit authentication and authorization, including policy/permission enforcement rather than authentication alone.
- Treat missing authorization on administrative endpoints as high priority.
- Check routes, verbs, status codes, response envelopes, validation errors, rate limits, and cancellation-token propagation against the neighboring controller.
- Identify breaking API contract changes.
- Reject logging or returning passwords, raw refresh tokens, raw API keys beyond their one-time creation response, signing keys, SMTP credentials, or sensitive audit values.
- Preserve enumeration-resistant authentication responses when that is the established rule.

## Business behavior

- Confirm rule ownership and enforcement location from module documentation and source.
- Look for duplicated or conflicting validation across Data Annotations, FluentValidation, handlers, EF constraints, and Identity options.
- Check active/deleted/status transitions and idempotency behavior.
- Require regression tests for confirmed business-rule changes.

## EF Core and persistence

- Use no tracking for reads and tracking for mutations unless there is a documented reason otherwise.
- Check soft-delete filters; do not expose deleted data accidentally.
- Look for N+1 queries, unbounded queries, unsafe `IQueryable` exposure, missing indexes, and incorrect delete behavior.
- Verify unit-of-work/transaction behavior, including rollback or disposal on every early return and exception path.
- Review migrations for destructive changes, nullable-to-required conversions, data backfills, indexes, keys, foreign keys, and the correct DbContext/startup project.
- Never apply a migration during review.

## Auditing and errors

- Ensure `IAuditableEntity` use is deliberate and key generation is compatible with the interceptor.
- Mask properties carrying sensitive values.
- Use explicit audit logging for reads or security events when the module requires it.
- Confirm `FluentResults` errors map to the intended HTTP response and do not expose internal exceptions.

## Tests and verification

- Match xUnit/Moq conventions in `ahis.template.test` unless an approved test strategy introduces another test type.
- Cover success, validation, not-found/conflict, authorization, status transition, persistence, and cancellation paths proportional to the change.
- Run affected tests first, then build and test the solution.
- Report warnings and failures accurately; do not hide pre-existing warnings.
