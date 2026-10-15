# Testing Strategy

`ahis.template.test` uses xUnit, Moq, FluentAssertions, and coverlet. It contains handler and Identity-service tests, relational LocalDB tests, and an ASP.NET Core test host for security-policy checks. Country handler tests establish success/empty behavior, projection/order, and selected collaborator calls; repository soft-delete behavior is simulated by a mock, not verified against EF.

`IdentityPolicyTest` runs the actual JWT bearer middleware, API-key handler, policies, and controller attributes against a disposable LocalDB-backed Identity store. Its mediator supplies test-only responses for authorized Country and administrative reads. `IdentityConcurrencyTest` runs selected role and session operations through independent Identity scopes and SQL connections. These tests do not prove all transaction interleavings or any deployed database state. See the [Identity security test runbook](../guides/identity-security-test-runbook.md) for fixture isolation, commands, covered cases, and limits. Choose the test layer based on the change rather than cloning the existing mock pattern; Identity, API-key, authorization, transaction, query-filter, audit, and migration changes often need more than a handler unit test.

Verify narrow to broad, substituting the affected project and test filter:

```powershell
dotnet build .\<affected-project>\<affected-project>.csproj --no-restore
dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~<affected-test>"
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

The Country example is `FullyQualifiedName~ahis.template.test.TestFeatures.CountryFeature.GetAllCountryQueryTest`; it is not a default check for unrelated changes. If no relevant focused test exists, report that limitation and run the affected project/solution checks instead. Restore first only when required. Build/test create local outputs and may use the network. Never use database-update commands as verification.

For the security-policy and concurrency suite, use `--filter "FullyQualifiedName~IdentityPolicy|FullyQualifiedName~IdentityConcurrency"`. The 2026-09-30 run passed 32 focused tests and 94 solution tests on SQL Server 2019 LocalDB `15.0.4382.1`.

`IdentityConcurrencyTest` also contains effective-Superadmin races for two disables, disable versus role replacement, and self-deactivation versus role replacement. These new cases require disposable SQL Server LocalDB fixtures; compilation alone does not verify their transaction interleavings.
