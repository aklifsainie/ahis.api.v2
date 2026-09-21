---
name: debug-api-error
description: Diagnose an AHIS API Template API failure through its real request path and propose the smallest evidence-backed fix without editing until authorized.
---

# Debug API Error

Work read-only first. Gather the route, request, response/status, exception or logs, environment, and reproduction evidence available. Read root/module instructions and trace the actual path through authentication/authorization/rate limiting, controller, custom mediator/handler or direct service, repository/service, DbContext or Identity manager, mapping, errors, and auditing.

Report symptom, evidence, root cause, affected files, configuration/data dependencies, smallest proposed fix, and regression scenario. Separate confirmed cause from uncertainty and do not copy known module risks as a default. If any code, schema, configuration, security, or behavior change is needed, route it through `change-blueprint` and the applicable approval gate.
