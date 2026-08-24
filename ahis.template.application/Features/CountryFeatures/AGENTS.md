# Country Feature Instructions

## Purpose and ownership

Country owns reference-data CRUD for country names and ISO-style codes. Its Application feature folder is the orchestration root, but the module spans Domain, Infrastructure, API, and tests.

## Owned code

- Entity/ViewModel: `ahis.template.domain/Models/Entities/Country.cs`, `Models/ViewModels/CountryVM/CountryVM.cs`
- Application: this folder and `Interfaces/Repositories/ICountryRepository.cs`
- Infrastructure: `Configurations/Entities/CountryConfiguration.cs`, `Repositories/CountryRepository.cs`, generic repository, `ApplicationDbContext`
- API: `ahis.template.api/Controllers/v1/CountryController.cs`
- Tests: `ahis.template.test/TestFeatures/CountryFeature`

## Established flow

Use custom CQRS handlers with direct `ICountryRepository` access; a Country service is not currently justified for single-record CRUD. Mutations save through `IUnitOfWork`. Reads are no-tracking, exclude soft-deleted records through the repository, add `IsActive`, project to `CountryVM`, and explicitly audit read activity.

## Persistence and rules

- `Country` inherits `BaseEntity` and implements `IAuditableEntity`.
- Full name, alpha-2, alpha-3, and numeric ISO code are unique.
- Normalize text by trimming and alpha codes by uppercasing before comparison/persistence.
- Soft deletion sets both `IsDelete=true` and `IsActive=false`.
- Do not physically delete Country unless a separately approved rule requires it.

## Before modifying

Read `docs/` in this folder, `CountryController`, all Country commands/queries, `ICountryRepository`, `GenericRepository`, `CountryConfiguration`, `BaseEntity`, the audit interceptor, and existing Country tests. Produce the repository-wide architectural blueprint and wait for approval.

## Testing

Cover success, invalid IDs/fields, uniqueness conflicts, inactive/deleted visibility, projection, sorting, auditing, and cancellation as applicable.
