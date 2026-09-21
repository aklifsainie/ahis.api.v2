# Testing Strategy

`ahis.template.test` uses xUnit, Moq, FluentAssertions, and coverlet. The committed suite contains two `GetAllCountryQueryHandler` unit tests. They establish handler success/empty behavior, projection/order, and selected collaborator calls; repository soft-delete behavior is simulated by the mock, not verified against EF.

There are no committed integration, controller, authentication, authorization, EF/database, or migration tests. Choose the test layer based on the change rather than cloning the existing mock pattern. Identity, API-key, authorization, transaction, query-filter, audit, and migration changes often need more than a handler unit test.

Verify narrow to broad, substituting the affected project and test filter:

```powershell
dotnet build .\<affected-project>\<affected-project>.csproj --no-restore
dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~<affected-test>"
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

The Country example is `FullyQualifiedName~ahis.template.test.TestFeatures.CountryFeature.GetAllCountryQueryTest`; it is not a default check for unrelated changes. If no relevant focused test exists, report that limitation and run the affected project/solution checks instead. Restore first only when required. Build/test create local outputs and may use the network. Never use database-update commands as verification.
