# Request Lifecycle

## Country

```text
CountryController -> custom IMediator -> command/query handler
-> ICountryRepository -> CountryRepository / GenericRepository
-> ApplicationDbContext -> SQL Server
```

Mutations save through `IUnitOfWork`; reads use no tracking, include active-state filtering in handlers, and attempt explicit view audits. The EF interceptor writes mutation audits with the application transaction.

## Account and Authentication

```text
AccountController or AuthenticationController -> custom mediator -> handler
-> IAccountService or IAuthenticationService
-> UserManager / SignInManager / IdentityContext / SMTP as applicable
```

Identity services own multi-step confirmation, password, 2FA, token, and refresh workflows. Inspect all related actions when a change affects cookies, current-user access, lockout, active/deleted state, or JWT validation.

## API client and audit

```text
ApiClientController -> IApiClientService -> ApplicationDbContext

AuditLogController -> custom mediator -> GetAuditLogQueryHandler
-> IAuditLogRepository.GetQueryable() -> EF query in Application
```

The first is a direct-service administrative variation. The second couples Application to EF query execution. Preserve them as explicit exceptions rather than copying either flow by default.
