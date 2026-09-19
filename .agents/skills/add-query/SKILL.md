---
name: add-query
description: Design and, after approval, add a read-only custom CQRS query and handler following repository projection, no-tracking, pagination, auditing, and result conventions.
---

# Add Query

Read root/module instructions and a neighboring query. Use the repository's custom mediator types.

Before editing, determine:

- Target module, filters, ordering, projection, and result type
- Repository/service ownership and whether an existing query method suffices
- No-tracking behavior and soft-delete/active filters
- Pagination limits when applicable
- ViewModel placement and mapping
- Explicit read auditing requirements
- Validation, not-found/empty behavior, logging, errors, and cancellation
- Controller/API impact, files, tests, and documentation

Present a blueprint and wait for explicit approval. After approval, implement and test result content plus important dependency/audit calls. Avoid materializing unbounded data when a paginated pattern is appropriate.
