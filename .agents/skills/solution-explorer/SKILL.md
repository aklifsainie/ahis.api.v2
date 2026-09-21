---
name: solution-explorer
description: Read-only analysis for locating an AHIS API Template change owner, analogous implementation, request path, rules, and risks; it does not modify files.
---

# Solution Explorer

Read the root and applicable nested `AGENTS.md`, then inspect source rather than inferring from names. Identify the owning module, projects, entry point, closest analogous implementation, persistence/service boundary, validation/authorization/audit behavior, relevant tests, and documentation.

Trace only steps that exist:

```text
HTTP/controller -> custom mediator/handler or direct service
-> repository/service -> DbContext or Identity manager -> response/error/audit
```

Return confirmed source facts separately from observed invariants, inferences, known risks, and open questions. Call out Country's repository pattern, Identity service flows, API-client direct-service variation, and audit `IQueryable` exception when relevant. Do not edit code, settings, migrations, or documentation.
