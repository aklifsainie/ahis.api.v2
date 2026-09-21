---
name: review-change
description: Review an AHIS API Template plan or diff for architecture, business-rule, security, persistence, and test risks without editing or auto-refactoring.
---

# Review Change

Read root/module instructions, relevant architecture and module documentation, and `REVIEW.md`. Review the actual change and comparable source—not an idealized architecture.

Use `architecture-reviewer` for dependency, boundary, persistence, public-contract, integration, and cross-cutting risks; `business-rule-reviewer` for state, validation, authorization, and rule provenance; and `test-reviewer` for behavior coverage and focused verification when their scopes are material. Reconcile findings by severity and evidence.

Return findings with impact, source locations/symbols, observed behavior, risk, confidence, and recommended disposition. Do not edit, auto-refactor, invent requirements, claim tests passed without execution, or apply migrations.
