---
name: write-tests
description: Plan and, when approved as part of a code change, add tests using this repository's xUnit and Moq conventions and an appropriate unit or integration boundary.
---

# Write Tests

Read root/module instructions, `docs/architecture/testing-strategy.md`, the changed behavior, and closest tests.

First provide a test plan covering:

- Behavior/rule and risk being protected
- Unit versus integration level and why
- Subject, collaborators, fixtures/mocks, and data
- Success, validation, not-found/conflict, authorization, status, audit, transaction, and cancellation cases that apply
- Exact test files and any required infrastructure

When tests accompany an approval-gated code change, include this plan in its blueprint and do not write tests before that blueprint is approved. For an explicitly requested test-only change, confirm the plan before substantial new infrastructure.

Use xUnit and existing Moq style by default. FluentAssertions is available but current tests also use xUnit assertions. Test observable behavior, not private methods or generated wording. Run affected tests, then the broader suite.
