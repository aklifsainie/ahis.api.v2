---
name: create-domain-feature
description: Analyze and, after approval, implement a business-rule or domain-behavior change in its established enforcement layer with regression coverage and documentation.
---

# Create Domain Feature

Read root/module instructions and business-rule docs. Trace the current rule through controller, handler, service/repository, entity/configuration, and tests.

Before editing:

- Identify the existing rule ID or propose a new stable module-prefixed ID
- Classify current and requested behavior as confirmed, inferred, or uncertain
- Find every enforcement location and call site
- Choose the established enforcement layer; avoid duplicate enforcement unless needed at distinct trust boundaries
- Assess status, persistence, migration, API, integration, security, and backward-compatibility impact
- List exact files and regression tests
- State repository/service decision

Present a blueprint and wait for explicit approval. After approval, implement, add regression tests, update the owning business-rules/use-case/status docs, and verify.
