# Country Integration Points

- `CountryController`: HTTP boundary; accepts Bearer or API-key authentication.
- `SimpleMediator`: validation and handler dispatch.
- `ICountryRepository`/`CountryRepository`: persistence boundary.
- `IUnitOfWork`: mutation save boundary.
- `IAuditLogger`: explicit read audits.
- `AuditSaveChangesInterceptor`: automatic create/update audits.
- `CountryReadPolicy` and `CountryWritePolicy`: configured in `Program.cs` but not currently attached to controller actions.

No external service integration exists for Country.
