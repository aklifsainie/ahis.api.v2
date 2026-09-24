---
name: change-blueprint
description: Create and save one evidence-backed Markdown plan for an AHIS API Template change before implementation; it does not implement.
---

# Change Blueprint

Use this skill when a request asks for planning, design, or a blueprint for a repository change. Start with `solution-explorer` work. Read root/module instructions, the closest implementation, and relevant architecture/module docs. Preserve the repository's demonstrated pattern rather than imposing a preferred architecture.

## Create the blueprint

1. Identify the owning module, affected behavior, applicable business rules, contracts, persistence, integrations, tests, and documentation.
2. Create one complete Markdown blueprint under `docs/blueprints/` with a unique `YYYY-MM-DD-short-feature-slug.md` filename. Do not overwrite an existing blueprint; use a dated revision suffix when revising one.
3. Include the applicable sections below and label confirmed facts, observed invariants, inferences, assumptions, risks, and open decisions.
4. Stop after saving the blueprint. Do not edit implementation code, create migrations, apply a database update, commit, push, deploy, or publish.

Include these sections, omitting only those that do not apply:

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

## Approval and response

The saved Markdown file is the blueprint of record. Approval applies only to the reviewed file revision. If it changes materially after approval, save and obtain approval of a revised blueprint before implementation.

After confirming the file exists, reply only:

`Blueprint completed and saved to '<repository-relative-path>'.`

Do not repeat the blueprint content in chat.
