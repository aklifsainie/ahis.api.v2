# Country Feature Instructions

Country owns reference-data CRUD for country names and ISO-style codes. It spans its Application feature, Domain entity/view model, Infrastructure repository/configuration/context, API controller, and handler tests.

Use the custom mediator with `ICountryRepository` and `IUnitOfWork` for simple Country CRUD. Reads are no-tracking, repository queries exclude soft-deleted rows, handlers add active-state filtering, and reads attempt explicit audit logging. Mutations are automatically audited through the EF interceptor.

Before changing Country, inspect the closest controller action, command/query and validator, `ICountryRepository`, generic repository, configuration, `BaseEntity`, audit interceptor, and [the module guide](../../../docs/modules/country.md).

Observed risks: Country permission policies are configured but not applied; unfiltered unique indexes keep soft-deleted values reserved while handler pre-checks exclude them; logical delete is audited as an EF update; explicit read-audit persistence is best-effort. Preserve these distinctions in any plan.
