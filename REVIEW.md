# Repository Review Guide

Review changes against the owning module, its `AGENTS.md`, and the closest active example. Prioritize evidence-backed correctness, security, behavior regression, and missing verification over style.

## Boundaries and contracts

- Check project-reference direction and ownership; do not normalize the non-strict Application-to-Identity dependency during unrelated work.
- Require a concrete reason for direct `ApplicationDbContext` use, direct controller services, or exposed `IQueryable`; API-client administration and audit queries are known exceptions.
- Verify that custom mediator requests/handlers—not MediatR—are used where the module follows mediator dispatch.
- Check public routes, response/error shapes, cancellation, compatibility, and configuration boundaries against neighboring endpoints.

## Security and behavior

- Verify actual authorization policies, not authentication alone. Administrative, setup, Country, and audit paths require particular scrutiny.
- Check that public setup or recovery operations are bound to their intended user and that sensitive material is neither logged nor returned.
- Trace status, lockout, active/deleted, JWT, refresh-token, cookie, security-stamp, and 2FA effects across every relevant path; do not assume a service call invalidates sessions unless validation enforces it.
- Reconcile validations and constraints across attributes, FluentValidation, handlers, Identity options, and EF indexes.

## Persistence, auditing, and tests

- Check tracking, soft-delete filters, uniqueness, pagination, transactions, early returns, migration context, and destructive/data risks.
- Ensure auditable entities have keys available when the interceptor runs, and distinguish transactional automatic audits from best-effort explicit audits.
- Select unit, integration, controller, or persistence coverage proportionate to the risk. The current suite is narrow Country-handler coverage; do not claim broader coverage.
- Report evidence, uncertainty, and validation results accurately. Never apply a migration during review.
