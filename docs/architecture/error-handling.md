# Error Handling

## Established mechanisms

- Application operations commonly return FluentResults `Result` or `Result<T>`.
- `ValidationError` maps to HTTP 400 with `ValidationProblemDetails` in `BaseApiController`.
- `EntityNotFoundError` maps to HTTP 404.
- `ConflictError` maps to HTTP 409.
- Unrecognized errors map to a generic HTTP 500 ProblemDetails response.
- Country uses `BaseApiController` consistently except for explicit create/delete status handling.
- Account and Authentication controllers frequently construct validation responses manually.
- API-client administration catches service exceptions directly and returns anonymous error objects.
- Audit controllers currently return `Ok(Result<T>)` rather than unwrapping through the base controller.

These are real variations. New endpoints should follow their module's dominant behavior and must not expose exception details or secrets. A proposal to unify error handling is a refactor and requires its own blueprint and approval.

## Validation

The custom `SimpleMediator` runs all registered FluentValidation validators before resolving a handler, but many request types also use Data Annotations and some handlers validate manually. Inspect every applicable layer because rules may be duplicated or inconsistent. Do not remove a validation path as incidental cleanup during feature work.
