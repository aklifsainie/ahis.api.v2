# Request Lifecycle

## Repository-based Country request

```text
HTTP request
  -> CountryController
  -> custom IMediator.Send
  -> SimpleMediator validation and handler resolution
  -> Country command/query handler
  -> ICountryRepository
  -> CountryRepository / GenericRepository
  -> ApplicationDbContext
  -> SQL Server
```

Mutations call `IUnitOfWork.SaveChangesAsync`. Reads use no tracking and may call `IAuditLogger` explicitly. Responses normally pass through `BaseApiController`, with create/delete status handling in the controller.

## Identity service request

```text
HTTP request
  -> AccountController or AuthenticationController
  -> custom mediator
  -> command/query handler
  -> IAccountService or IAuthenticationService
  -> UserManager / SignInManager / IdentityContext
  -> SQL Server and, where applicable, SMTP
```

Identity services own multi-step workflows such as confirmation tokens, password operations, 2FA, JWT creation, refresh-token rotation, and transactions.

## API-client administration request

```text
HTTP request
  -> ApiClientController
  -> IApiClientService
  -> ApiClientService
  -> ApplicationDbContext
  -> SQL Server
```

This is an existing variation: request types implement `IRequest<Result<T>>`, but there are no handlers and the controller calls the service directly. Do not copy the variation without explaining why it fits the requested change.

## Audit-log query

`AuditLogController` sends `GetAuditLogQuery` to a handler, which obtains an `IQueryable` from `IAuditLogRepository`, filters, orders, paginates, and projects to `AuditLogVM`.
