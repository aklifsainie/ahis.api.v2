---
name: debug-api-error
description: Diagnose an API failure by tracing the request through this repository's controller, custom mediator, handler, service or repository, DbContext, and SQL-facing configuration; do not edit until a fix blueprint is approved.
---

# Debug API Error

Start read-only. Read root/module instructions and gather the route, request, response/status, logs/exception, environment, and reproduction evidence available.

Trace:

```text
Request -> authentication/authorization/rate limit -> controller
-> custom mediator/handler or direct service -> repository/service
-> DbContext -> EF configuration/migration/database assumptions
```

Identify symptom, root cause, evidence, affected files, configuration/data dependencies, and a regression test. Distinguish the root cause from secondary warnings.

If no code change is needed, report the operational/configuration resolution. If code, schema, or configuration must change, produce the smallest architectural fix blueprint—including service/repository decision, risks, files, tests, and docs—and wait for explicit approval before editing.
