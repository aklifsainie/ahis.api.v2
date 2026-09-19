# Country Module Overview

Country is reference data exposed through authenticated CRUD endpoints at `api/v1/country`. It owns a single `Country` entity and `CountryVM`. The module uses the repository/unit-of-work form of the solution's custom CQRS pattern.

```text
CountryController
  -> custom mediator
  -> Country command/query handler
  -> ICountryRepository
  -> CountryRepository / GenericRepository
  -> ApplicationDbContext
```

Read queries explicitly write View audit events. EF mutations are automatically audited because Country implements `IAuditableEntity`.
