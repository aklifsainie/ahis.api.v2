# Testing Strategy

## Current state

`ahis.template.test` uses xUnit, Moq, FluentAssertions, and coverlet. The current suite contains two unit tests for `GetAllCountryQueryHandler`; they mock the repository, logger, and audit logger and verify results plus important dependency calls.

There are no committed integration, controller, authentication, EF/database, or migration tests.

## Expectations for changes

- Inspect the closest existing tests before choosing naming and setup style.
- Test confirmed business rules and regression paths, not implementation trivia.
- Handler changes should normally cover success plus relevant validation, not-found, conflict, empty-result, audit, and cancellation behavior.
- Security changes should cover authentication/authorization and avoid assertions containing raw secrets.
- Persistence changes should test query filters, tracking, uniqueness/status behavior, and transactions at the appropriate level. Propose integration infrastructure before adding it.

## Verification order

1. Build the affected project.
2. Run the affected tests.
3. Build `AhisApiTemplate.sln`.
4. Run the solution tests.

Use `--no-restore` after a successful restore. Do not use database update commands as verification.
