---
name: trace-request
description: Trace an existing HTTP or internal request through controller, custom mediator, handler, service or repository, DbContext, mapping, errors, auditing, and tests without modifying code.
---

# Trace Request

Perform read-only analysis. Read root/module instructions, then follow actual symbols and registrations rather than assuming the default flow.

Trace present steps from:

```text
HTTP route
  -> controller/action
  -> request model and custom mediator
  -> handler
  -> service or repository contract
  -> implementation
  -> DbContext and EF configuration
  -> entity/table
```

Also identify validation, authorization, rate limiting, mapping/ViewModel, result-to-HTTP conversion, logging, auditing, transaction/tracking, cancellation, configuration, and tests. Explicitly say which conceptual steps are absent or bypassed. Return file paths and relevant symbols. Do not edit files.
