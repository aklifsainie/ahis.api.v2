# Identity security policy and concurrency test runbook

Last verified: 2026-09-30. Blueprint: [Identity security pending verification](../blueprints/2026-09-30-identity-security-pending-verification.md).

## Prerequisites and isolation

- Windows with .NET SDK 8 or later and the .NET 8 ASP.NET Core runtime. The verified machine used SDK `9.0.311` and runtime `8.0.30`.
- SQL Server LocalDB instance `(localdb)\MSSQLLocalDB`. The verified engine was **Microsoft SQL Server 2019 Express LocalDB, 15.0.4382.1 (RTM-CU27-GDR)**, obtained with `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION"`.
- No configured application, Identity, SMTP, or external service is needed. The HTTP host overrides `ConnectionStrings:DefaultConnection` to a unique `AhisIdentityPolicy_<GUID>` LocalDB database before startup. It replaces startup role bootstrap, API-key validation, Country/role query dispatch, and email delivery with test-only implementations. Real JWT bearer middleware, API-key authentication handler, authorization policies, Identity managers, `IdentityContext`, token-state validation, and Identity service transactions still run.
- `IdentityPolicyHost.InitializeAsync` creates only its unique database with EF `EnsureCreatedAsync`; `DisposeAsync` removes it with `EnsureDeletedAsync` after verifying the `AhisIdentityPolicy_` prefix. These tests do **not** call EF migration commands or use the repository's configured connection value. They do not open `ApplicationDbContext`; supported controller actions use test-only mediator responses. Never point the fixture at another database or remove the prefix guard.
- A no-op test email sender prevents SMTP. API keys in these tests are fixed fake values validated by an in-process test validator; no external API client is touched. The test host signs local-only JWTs with a test-only key.

## Exact commands

Run from the repository root:

```powershell
sqllocaldb info MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION"
dotnet restore .\AhisApiTemplate.sln --verbosity quiet
dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~IdentityPolicy|FullyQualifiedName~IdentityConcurrency" --verbosity quiet
dotnet build .\AhisApiTemplate.sln --no-restore --verbosity quiet
dotnet test .\AhisApiTemplate.sln --no-build --no-restore --verbosity quiet
```

Verified on 2026-09-30: LocalDB was running; restore succeeded; focused tests **32 passed, 0 failed, 0 skipped**; build succeeded with **0 warnings and 0 errors**; full solution tests **94 passed, 0 failed, 0 skipped**. A final `sys.databases` query found no remaining `AhisIdentityPolicy_` databases.

## Scenarios and expected outcomes

| Tests | Expected outcome |
| --- | --- |
| `IdentityPolicyTest` permission catalog, role/user-role reads, and role mutations | Bearer Superadmin can read; absent bearer, ordinary `User`, and API key cannot obtain administrative data or invoke role/permission/assignment writes. Sensitive catalog response has `Cache-Control: no-store`. |
| `IdentityPolicyTest` Country policy matrix | Bearer roles, signed `human_permission` and API-client `api_permission` grant only their cataloged read/write actions. A bad bearer plus a valid API key is rejected instead of falling back to the key. |
| `IdentityPolicyTest` real password login | A direct user claim of reserved type `human_permission` does not enter the issued token; an allow-listed role claim does and authorizes Country write. |
| `IdentityPolicyTest` permission replacement and session revocation | A changed role permission invalidates the old bearer; a new login has the updated authorization. Ending the bearer session makes that token fail. |
| `IdentityPolicyTest` authenticated security limiter | Five catalog reads for one actor succeed in its window; the sixth returns `429`. |
| `IdentityConcurrencyTest` cross-demotion | Two separate Identity scopes start opposing Superadmin removals together; exactly one commits and one effective Superadmin remains. |
| `IdentityConcurrencyTest` permission change versus demotion | Competing Identity scopes preserve at least one Superadmin; a committed permission change removes its claim and revokes assigned user's active sessions. |
| `IdentityConcurrencyTest` refresh versus revoke-one/revoke-all | After the competing calls settle, no active session or refresh token remains for the user; any token returned by a winning refresh is rejected by bearer validation. |
| `IdentityConcurrencyTest` stale role ETag and repeat revoke | A stale ETag cannot overwrite a committed rename; repeated owner-session revocation succeeds without restoring tokens or sessions. |

Existing focused role-management tests also cover rollback when target invalidation fails and post-commit audit failure; they ran in the full suite. The new HTTP host uses mediator responses for authorized Country and administrative reads, so those tests prove routing through the actual authentication/authorization middleware and controller attributes, **not** Country persistence or administrative response data from a production handler.

## TDD record and evidence limits

The first catalog test failed because the host had no connection; the fixture then supplied only a GUID-named test connection. The role-catalog and assignment-read tests failed because the test mediator had no response for those authorized actions; the minimum test-only responses made them pass. Concurrency, login-provenance, and stale-tag tests were added before their needed fixture helpers; their first build failed for missing helpers, then the focused tests passed after those helpers were added. The mutation-policy tests initially failed because the test helper disposed request content before the asynchronous send completed; fixing that test-only lifetime made them pass. Several characterization cases passed on their first execution because production already had the expected behavior. No production behavior was changed to force a red test.

The concurrency tests synchronize **start**, use independent DI scopes and SQL connections, and inspect committed state. They do not deterministically pause each operation inside its transaction. The LocalDB result demonstrates the tested interleavings on SQL Server 2019 LocalDB only; it does not prove every possible schedule, multi-instance deployment, or another SQL Server edition/version. Automated capture/assertion of sensitive log and audit payloads was not added in this slice; existing handler tests cover selected post-commit audit failures, but full HTTP audit redaction remains unverified. The simultaneous demotion response policy (retry versus conflict) remains an owner decision. CI provisioning for a disposable SQL Server backend is also undecided. No migration was applied and no deployed database was inspected.
