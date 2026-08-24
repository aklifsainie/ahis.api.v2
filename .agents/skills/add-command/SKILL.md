---
name: add-command
description: Design and, after approval, add a state-changing custom CQRS request and handler consistent with the owning module.
---

# Add Command

Read root/module instructions and at least one neighboring command and controller action. This repository uses custom `IRequest` and `IRequestHandler`, not MediatR.

Before editing, identify:

- Target module and mutation rule
- Request/result shape and validation layers
- Handler placement and naming
- Repository versus service dependency, with explicit service decision
- Tracking, unit-of-work/transaction, auditing, logging, error, and cancellation behavior
- Call sites/controller changes
- Exact files and regression tests
- Database and documentation impact

Present a blueprint and wait for explicit approval. After approval, implement the smallest command flow and test success plus relevant validation/not-found/conflict/status paths.
