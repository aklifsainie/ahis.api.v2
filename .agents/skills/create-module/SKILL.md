---
name: create-module
description: Design and, after explicit approval, create a genuine business module that follows this repository's existing project and feature conventions.
---

# Create Module

Read root `AGENTS.md`, `docs/architecture`, and the closest related module instructions. Use `solution-explorer` principles to verify that a new module boundary is justified rather than an extension or submodule of existing ownership.

Before implementation, determine:

- Business purpose, use cases, and data ownership
- Related modules and integration boundaries
- Similar existing module and pattern
- Projects/folders required; do not create a project merely because a concept has a name
- Entity, EF configuration, ViewModel, repository, service, custom CQRS, controller, endpoint, migration, test, and documentation impact
- Service necessity with an explicit Yes/No reason
- Confirmed, inferred, and uncertain rules

Produce an architectural blueprint with exact files to add/modify and wait for explicit approval. Do not edit implementation before approval.

After approval, implement the smallest coherent module, create its `AGENTS.md` and meaningful docs, update root maps only where inaccurate, add tests, and verify affected projects before the solution. Do not apply database migrations unless separately requested.
