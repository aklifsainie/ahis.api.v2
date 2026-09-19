---
name: review-module
description: Review a module for correctness, security, layer and boundary violations, persistence/query issues, validation gaps, and missing tests; report findings without refactoring.
---

# Review Module

Read root `AGENTS.md`, `REVIEW.md`, owning module instructions/docs, project dependencies, and representative call paths. Review only; do not modify code.

Check:

- Wrong-layer dependencies and module ownership
- Business logic in controllers and inappropriate direct DbContext use
- Repository/service choice, overloaded handlers, duplicate rules
- Authentication, permission authorization, secret handling, sensitive logging
- Validation and result/HTTP mapping gaps
- Tracking, soft delete, N+1/unbounded queries, indexes, transactions, migrations
- Auditing correctness and sensitive masking
- Cancellation and UTC timestamp behavior
- Missing or weak tests and documentation drift

Report findings ordered by severity with exact file/line evidence, impact, and smallest recommended direction. Separate confirmed defects from risks or uncertainties. Do not perform fixes automatically; a fix requires the architectural approval workflow.
