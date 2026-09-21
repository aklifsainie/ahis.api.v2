---
name: change-blueprint
description: Create one evidence-backed plan for an AHIS API Template change, including architecture, behavior, security, persistence, migration, test, and documentation impact; it does not implement.
---

# Change Blueprint

Start with `solution-explorer` work. Read root/module instructions, the closest implementation, and relevant architecture/module docs. Preserve the repository's demonstrated pattern rather than imposing a preferred architecture.

Produce this blueprint, omitting inapplicable sections:

```text
Outcome:
Owning module and analogous implementation:
Observed flow and proposed flow:
Files to add / modify / intentionally leave unchanged:
Business behavior and confidence:
Authorization and security:
API contract:
Entity, persistence, and migration impact:
Service decision and evidence:
Transaction, audit, and integration impact:
Test plan:
Documentation impact:
Validation commands:
Risks, assumptions, and open questions:
```

Request explicit approval before architecture, module-boundary, public-API, business-behavior, authorization/security, schema/migration, dependency, integration, production-configuration, or broad-refactor changes. Small local work may proceed only when the active request already authorizes it and no such boundary applies. Migration generation and migration application remain distinct decisions.
