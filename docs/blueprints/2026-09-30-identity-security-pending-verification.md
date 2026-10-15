# Blueprint: Identity security policy and concurrency verification

**Status:** Proposed for approval. Scope is tests and any test-only host or database fixture. A production behavior change discovered by these tests requires its own revised blueprint.

## Outcome

Close the backlog's missing automated HTTP authorization coverage for role and human-user permissions, and its relational concurrency verification for session revocation, role replacement, and concurrent Superadmin demotion. This is a distinct blueprint because it changes no intended public contract or schema and provides a reusable verification foundation for the other security work.

## Owning module and analogous implementation

**Confirmed:** `ahis.template.api/Program.cs` defines `IdentityRoleReadPolicy`, `IdentityRoleManagementPolicy`, `IdentityRoleAssignmentPolicy`, `CountryReadPolicy`, and `CountryWritePolicy`; `CountryController` applies the Country policies. `AuthenticationService` puts current roles and allow-listed `human_permission` role claims in bearer JWTs. `IdentityRoleManagementService` uses serializable Identity transactions for role changes. `AccountService` and `AuthenticationService` update `RefreshSessions` and hashed `RefreshTokens`. `ahis.template.test` currently references Application, Domain, and Identity, but not API; its project file lists xUnit and Moq, with no HTTP test-host package. Existing Identity service and handler tests, including `IdentityRoleManagementServiceTest` and `RevokeSessionCommandHandlerTest`, are the nearest examples.

## Observed flow and proposed flow

**Observed:** HTTP authorization is configured in `Program.cs` and enforced at controller actions. Existing tests cover selected handler and Identity-service paths but do not execute the HTTP authentication/authorization middleware. The backlog records LocalDB role-permission tests and manual runbook verification, with HTTP policy and concurrent-demotion coverage pending.

**Proposed:** Add a controlled HTTP test host that runs the actual policy definitions and controller attributes with test Identity data. Use a real SQL Server-compatible relational test database for concurrency cases; EF InMemory cannot establish transaction and isolation behavior. Exercise simultaneous requests with independent scopes/connections and deterministic barriers, then assert committed state and token/session consequences. Keep production policy definitions intact unless a failing test exposes a separately approved defect.

## Files to add, modify, or intentionally leave unchanged

- Modify `ahis.template.test/ahis.template.test.csproj` to reference API and the selected ASP.NET Core test-host package; add a relational fixture and targeted HTTP/concurrency test files under `TestFeatures/IdentityFeature`, `AccountFeature`, and `CountryFeature`.
- Add test-only configuration, predictable authentication principals, role/permission setup, and disposable database isolation. Use the real bearer and API-key paths where the assertion depends on scheme selection or token version; a fake handler alone would not prove those properties.
- Leave `Program.cs`, controllers, services, entities, migrations, and production configuration unchanged in this blueprint. If tests require a production seam, revise this blueprint before changing the seam.

## Business behavior and confidence

**Confirmed from code:** Country read allows bearer `User`/`Superadmin`, bearer `human_permission=country.read`, or API-client `api_permission=country.read`; Country write allows `Superadmin`, bearer `human_permission=country.write`, or API-client `api_permission=country.write`. An Authorization header selects bearer ahead of an API key. Changed role permissions invalidate assigned users' authorization state; direct role replacement prevents removal of the last effective Superadmin. Session revocation ends selected sessions or all target sessions as documented in the backlog.

**Test hypothesis:** Concurrent transactions cannot both commit an outcome that leaves no effective Superadmin, revives an ended session, or produces a token/session state inconsistent with the committed role change. Record any actual failure as a finding; do not silently change expected behavior.

## Authorization and security

Test `401` for missing/invalid bearer, `403` for a valid bearer without the required role, API-key denial on bearer-only administrator routes, `CountryReadPolicy`/`CountryWritePolicy` for both principal types, and bearer precedence with a bad bearer plus valid API key. Prove `human_permission` does not originate from arbitrary direct user claims or API-client claims. Prove old bearer credentials fail after role/permission changes and after session revocation. Assert no token, proof, password, or credential material enters test logs or snapshots.

## API contract

No new route or response shape. Tests exercise existing `GET /api/admin/identity/permissions`, role and user-role routes, Country reads/writes, `DELETE /api/account/sessions/{sessionId}`, and administrative/self-service revoke-all routes. Include policy metadata and actual middleware results, rate-limit behavior where the test clock/partition can be controlled, and no-store headers on sensitive responses.

## Entity, persistence, and migration impact

No production schema or migration. Test data must use isolated disposable stores and must never target the user's configured `DefaultConnection`. Confirm the existing migration set can create the fixture; do not use a database update against any shared or user database. If a local SQL Server test instance is unavailable, run HTTP cases and report relational concurrency as unverified rather than substituting EF InMemory.

## Service decision and evidence

No new production service. The test host crosses the real controller → mediator → Identity service path; relational tests invoke the current service from separate scopes. This keeps Country's repository pattern and Identity's service boundary unchanged.

## Transaction, audit, and integration impact

Test rollback/commit outcomes, idempotent session revocation, role ETag conflict, and post-commit audit failure behavior. Use a test audit sink or isolated Application database; do not send SMTP email or mutate an external application. Test-only stubs must make failures explicit and preserve the assertion being tested.

## Test plan

1. Build the isolated host and fixtures; prove one authorized and one denied request before adding a broad matrix.
2. Cover admin role, assignment, permission-catalog and Country policy outcomes, including stale JWT authorization state and scheme precedence.
3. Use coordinated concurrent operations for last-admin removal, role-permission/demotion collisions, revoke-one/revoke-all versus refresh rotation, and retry/idempotency. Assert database state and bearer/refresh behavior after each race; repeat only races that are timing-sensitive.
4. Run focused tests, then the solution suite. Document database engine/version, fixture setup, and any skipped concurrency scenario.

## Documentation impact

Update `docs/backlog/identity-security-endpoints.md` only for cases actually verified; correct `docs/architecture/testing-strategy.md` if the new test layer is committed. Preserve deployment-application caveats.

## Validation commands

From the repository root after approval: `dotnet restore .\AhisApiTemplate.sln`; `dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~IdentityPolicy|FullyQualifiedName~IdentityConcurrency"`; `dotnet build .\AhisApiTemplate.sln --no-restore`; `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`. Do not run `dotnet ef database update` against a user database.

## Risks, assumptions, and open questions

- **Risk:** The current API startup runs role bootstrap. The host needs an isolated database and deliberate bootstrap configuration to avoid a real target.
- **Open decision:** Choose a disposable SQL Server test backend and CI provisioning method. LocalDB results alone may not reproduce deployment topology.
- **Open decision:** Decide whether the expected concurrent demotion behavior includes a retry or a conflict response. The invariant is that at least one effective Superadmin remains.
- **Evidence boundary:** This blueprint validates source behavior; it does not independently verify application of migrations to any deployed database.
