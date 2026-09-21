# Country Module

## Ownership and flow

Country owns active reference-data CRUD for names and ISO-style codes. It uses `CountryController -> custom mediator -> handler -> ICountryRepository -> ApplicationDbContext`; mutations save through `IUnitOfWork`. The entity is `Country`, the public projection is `CountryVM`, and mutation auditing is automatic through the interceptor.

## Observed behavior

- List/GetById reads combine generic soft-delete filtering with handler-level active-state filtering, use no tracking, and attempt explicit view auditing.
- Add/update normalize text and alpha codes, validate formats, pre-check uniqueness, and persist updates. Delete sets `IsDelete=true` and `IsActive=false`.
- EF defines unique indexes for full name and all codes; the alpha-3 FluentValidation rule conflicts with stricter annotations/handler checks.

## Risks and test boundary

The indexes are unfiltered, so soft-deleted values remain reserved while handler pre-checks exclude deleted rows; conflicts can emerge at save time. Country policies are configured but unused. A logical delete produces an automatic audit update, and explicit read auditing is best-effort. Existing tests cover handler behavior only and simulate repository filtering.

These are observed facts rather than owner-confirmed rules. See the local `AGENTS.md`, [persistence](../architecture/persistence.md), and [auditing](../architecture/auditing.md).
