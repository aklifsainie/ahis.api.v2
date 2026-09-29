# Blueprint: API-key authorization hardening and RBAC alignment

Date: 2026-09-29. Status: Proposed for implementation approval. Approval applies to this file revision. This blueprint addresses all five findings in the [API-key review](../reviews/2026-09-29-api-key-auth-review.md); it does not report implemented fixes.

## Outcome and confirmed business decisions

Close anonymous API-client administration, enforce Country permissions, restrict audit access, isolate client rate limits, and prevent usage-tracking failures from breaking authentication. Add redacted administrative audit records as part of securing key management.

**User-confirmed decisions:** Superadmin manages API clients and reads audit logs. User and Superadmin can read Country; only Superadmin can change Country using JWT. API clients require the corresponding Country permission. Every API-client administrative mutation requires the existing five-minute, actor-bound `X-Step-Up-Proof`.

| Endpoint/action | Human bearer access | API-key access | Policy / additional check |
| --- | --- | --- | --- |
| POST `/api/apiclient` | Superadmin | Denied | `ApiClientAdminPolicy`, current role check, step-up |
| POST `/api/apiclient/{apiClientId}/keys` | Superadmin | Denied | Same |
| POST `/api/apiclient/{apiClientId}/keys/{keyId}/revoke` | Superadmin | Denied | Same |
| POST `/api/apiclient/{apiClientId}/deactivate` | Superadmin | Denied | Same |
| GET `/api/v1/country` and `/{id}` | User or Superadmin | `country.read` | `CountryReadPolicy` |
| POST/PUT/DELETE Country actions | Superadmin | `country.write` | `CountryWritePolicy` |
| Both GET `/api/auditlog` actions | Superadmin | Denied | `AuditReadPolicy` |

**Proposed defaults for approval:** `country.write` does not imply `country.read`; a client needing both receives both permissions. Users with no qualifying role and custom roles alone receive no access through these policies. API-client permissions never become Identity roles. Country writes do not gain a step-up requirement in this change.

## Owning modules, evidence, and service decision

**Confirmed source facts:**

- [ApiClientController](../../ahis.template.api/Controllers/v1/ApiClientController.cs) calls `IApiClientService` directly without authorization. [ApiClientService](../../ahis.template.infrastructure/Services/ApiClientService.cs) owns client/key changes through `ApplicationDbContext`. Preserve this documented local variation.
- [Program.cs](../../ahis.template.api/Program.cs) defines unused API-key Country permission policies and a shared ten-request `ApiPolicy`. [CountryController](../../ahis.template.api/Controllers/v1/CountryController.cs) and [AuditLogController](../../ahis.template.api/Controllers/AuditLogController.cs) accept either bearer or API-key authentication without resource permissions.
- [ApiKeyAuthenticationHandler](../../ahis.template.api/ApiClientAuthentication/ApiKeyAuthenticationHandler.cs) emits machine claims and awaits a usage write before returning success. [ApiKeyValidator](../../ahis.template.infrastructure/ApiClientAuthentication/ApiKeyValidator.cs) loads current permissions and client limits on each authentication, checks key/client eligibility, and updates `LastUsedAt`.
- [IdentityRoleManagementService](../../ahis.template.identity/Services/IdentityRoleManagementService.cs) is the analogous administrative guard: current Superadmin membership plus [IAccountSecurityProofService](../../ahis.template.identity/Interfaces/IAccountSecurityProofService.cs). Proof validation already checks actor binding, expiry, security version, and account eligibility through Identity services.
- API-client entities do not implement `IAuditableEntity`. The [audit interceptor](../../ahis.template.infrastructure/Persistences/Interceptors/AuditSaveChangesInterceptor.cs) therefore does not record their changes. [AuditLogger](../../ahis.template.infrastructure/Services/AuditLogger.cs) saves immediately and suppresses persistence errors; it cannot provide an atomic administrative audit.

**Ownership:** API owns schemes, policies, controller guards, response mapping, and rate-limit selection. Identity owns authoritative human role/proof checks. Application retains service contracts. Infrastructure owns API-key persistence and administrative audit writes. Domain retains the validation result and existing entities. Country's mediator/repository/unit-of-work flow and Audit's documented query exception remain in place.

**Service decision:** Add a narrow `IIdentityApiClientAdministrationService` and implementation in Identity with `ValidateAsync(string actorUserId, string? proof, CancellationToken)` returning `Result`. It checks current Superadmin membership through `UserManager<ApplicationUser>` and validates proof with the existing service. Return distinguishable `Forbidden`, `Validation`, and `OperationalFailure` metadata with generic messages. Register it in Identity DI. The API calls this guard before any API-client target lookup or mutation; Infrastructure does not gain an Identity context dependency. No mediator conversion or general authorization-framework refactor is included.

## Observed and proposed request flows

Current administration: HTTP -> optional bearer authentication -> shared limiter -> controller -> API-client service -> application save.

Proposed administration: HTTP -> bearer authentication and Superadmin policy -> per-user security limiter -> Identity role/proof guard -> direct API-client service -> one application save containing mutation and redacted audit -> existing success response.

Proposed Country: HTTP -> select one credential scheme -> authenticate -> action-specific role/permission policy -> selected actor's limiter -> existing mediator path.

Proposed audit read: HTTP -> bearer authentication and Superadmin policy -> per-user security limiter -> existing audit query.

## Authorization and security implementation

1. Add bearer-only `ApiClientAdminPolicy` and `AuditReadPolicy`, each requiring authentication and `IdentityRoleNames.Superadmin`. Apply the first at API-client controller level and the second at audit controller level. Replace their `ApiPolicy` limiter with the existing `AuthenticatedSecurityPolicy` (five requests per minute per human actor). Its allowance remains shared with that actor's other administrative security operations.
2. Apply `CountryReadPolicy` to both GET actions and `CountryWritePolicy` to POST, PUT, and DELETE. Remove the controller-level `Bearer,ApiKey` attribute so it cannot add schemes back into the action policies.
3. Add a policy scheme named `CountryAuthentication`, used only by the two Country policies. Its selector chooses Bearer whenever the `Authorization` header is present, otherwise ApiKey when `X-API-Key` is present, otherwise Bearer for the challenge. Use the same selection for authentication, challenge, and forbid. Keep the global default scheme Bearer.
4. Country policies evaluate only the selected authenticated principal: bearer requires the table's role; ApiKey requires its machine authentication marker and exact permission. Do not combine roles from one principal with permissions from another or require both a role and machine permission. Do not identify JWTs by assuming their `ClaimsIdentity.AuthenticationType` is literally `Bearer`; use the selected scheme.
5. When both headers are present, bearer takes precedence, including when it is empty or invalid. A failed bearer never falls back to the key. A valid ordinary User plus a write-capable key cannot write Country. Bearer-only administration and audit ignore API keys and require a valid bearer. This deliberately changes the previous either-scheme behavior and provides one unambiguous actor for audit and throttling.
6. Bind the optional step-up header on all four API-client actions; reject multiple header values as invalid proof. Extract the actor only from the authenticated bearer principal, invoke the Identity guard, then call the mutation service. Reject missing/currently removed Superadmin membership before target access. Preserve bearer session, restriction, and token-use checks already configured.
7. Make API-client service actor parameters non-null `string` values and reject blank actors defensively. These are internal service contracts; only the guarded controller supplies them. This validation is not a substitute for the HTTP and Identity authorization checks.

No global fallback policy is added: public registration/login/recovery routes need a separate inventory before such a change. No new key format, expiry mandate, permission-management route, or arbitrary role/claim editor is introduced. Unknown stored permission codes grant none of these protected operations.

## Rate-limit behavior

- Replace Country's `ApiPolicy` with `CountryTrafficPolicy`. Use a one-minute sliding window, four segments, no queue, and immediate `429` on exhaustion. Remove the old shared registration once no endpoints reference it.
- For API keys, populate a typed request feature after successful validation with the immutable database client GUID, key GUID, and validated `RateLimitPerMinute`. Use this server-created context only when ApiKey is the selected Country scheme; do not trust caller headers or an unscoped `NameIdentifier` as machine configuration.
- Use a partition key containing `api-client`, the client GUID, and the configured limit. Every key for the same client and limit shares one allowance across Country actions; distinct clients cannot consume each other's allowance. Including the limit makes an authorized configuration change take effect on the next request with a fresh allowance. There is no new limit-update endpoint in this blueprint.
- Enforce the existing 1–10000 range in the service as well as HTTP model validation. Invalid stored limits cause generic key authentication failure with a redacted operational log; do not silently grant an unlimited allowance.
- Bearer Country traffic uses `human-country:{userId}` with ten requests per minute. Keep its allowance separate from administrative security traffic. Anonymous or invalid credentials are rejected by authorization before Country quota consumption. Preserve the existing JSON `429` response and add `Retry-After` only when limiter metadata provides it.
- Keep authentication -> authorization -> endpoint rate limiting order. These endpoint quotas do not protect the database from invalid-key authentication floods; pre-authentication or ingress throttling is a distinct operational concern.
- **Explicit limit:** these quotas are per application instance and reset on process restart. No distributed limiter or external dependency is introduced. A deployment requiring an aggregate quota across replicas needs a separately revised design before claiming that guarantee.

## Usage tracking and availability

Treat `LastUsedAt` as approximate, best-effort telemetry. Extend the internal `ApiKeyValidationResult` with nullable `LastUsedAt`; return the loaded value from the existing validation query. Use the built-in `TimeProvider` for deterministic time checks.

Attempt an update only when the loaded value is null or at least one minute old. Update with a matching conditional predicate so concurrent requests cannot repeatedly rewrite a recently updated row. Keep `IApiKeyValidator.UpdateLastUsedAsync` as the persistence boundary and pass a cancellation token linked to request cancellation with a one-second tracking timeout. No background task may retain a scoped DbContext.

In the authentication handler, catch errors only around this optional update. Request cancellation propagates; a tracking timeout or other tracking failure records a safe event containing the key GUID and failure category, then authentication succeeds. Never swallow a failure in key lookup or eligibility validation. Do not log request headers, raw keys, hashes, proof, or arbitrary exception text that may contain sensitive data. `LastUsedAt` continues to mean successful authentication, not successful endpoint authorization or business processing.

## API contract, transactions, and audit

Routes, success bodies, creation `201`, mutation `204`, and existing target-not-found behavior remain. The deliberate contract changes are authorization enforcement, the required proof header, deterministic Country credential selection, and quota behavior.

| Situation | Response |
| --- | --- |
| Missing/invalid selected credential; API key alone on bearer-only endpoint | `401`; administrative service not invoked |
| Authenticated principal lacks required role/permission; current admin role removed | `403`; no mutation |
| Superadmin with missing, expired, mismatched, malformed, or multiple proof values | Generic `400` ProblemDetails before target lookup |
| Quota exhausted | `429`, existing JSON shape |
| Identity guard or persistence unavailable | Generic `500`; no partial mutation or sensitive exception response |

Set `Cache-Control: no-store` on all API-client responses, including failures and key-creation responses, and on audit-read responses. Implement this using endpoint-scoped response handling that also covers middleware short circuits. Document headers/statuses in Swagger; show Country bearer/API-key alternatives and bearer plus proof for administration without sample secret values.

Add explicit `AuditLog` rows inside `ApiClientService` before the same `SaveChangesAsync` as the mutation. Initial creation writes a client-created event and initial-key-created event; additional-key creation, actual revocation, and persisted deactivation each write their corresponding event. A revoke that already returns early remains idempotent with no duplicate mutation audit. Deactivation preserves its existing state behavior and records client ID and number of keys revoked. Use existing `Create`/`StatusChange` enum values plus fixed operation names in metadata.

Allowlisted event content: authenticated human actor ID, fixed operation, target database GUIDs, UTC timestamp, correlation ID, and revocation count where applicable. Keep old/new snapshots null; omit raw key, key hash, proof, tokens, contact information, and free-text reasons. The stored business reason remains on the existing business row. Do not make key entities generically auditable or call `IAuditLogger.LogAsync` inside this transaction. A failed audit insert rolls back the associated mutation; a raw key is returned only after successful save. This creates no cross-context transaction with Identity. Role changes cannot retract operations already authorized and in flight.

## Files and persistence impact

- API: modify `Program.cs`, `ApiClientController`, `CountryController`, `AuditLogController`, `ApiKeyAuthenticationHandler`; add narrowly scoped scheme/policy/rate-limit helpers and request context under the API project. Reuse existing default/claim constants. Preserve unrelated working-tree changes in Program.
- Identity: add the narrow administration guard interface/implementation and DI registration. Reuse existing proof, role-manager/user-manager, and token-state services; do not change token issuance, role mutations, or bootstrap behavior.
- Application/Domain/Infrastructure: strengthen `IApiClientService` actor arguments, extend `ApiKeyValidationResult`, update `ApiKeyValidator` and `ApiClientService`, and inject `TimeProvider` where needed. Reuse the existing clients, keys, permissions, and audit tables.
- Tests: add HTTP authentication/authorization coverage under `ApiKeySecurity`, persistence tests for API-key validation, usage updates, and administrative audit atomicity, and narrow Identity guard tests. Add explicit API and Infrastructure references to the test project plus test-only `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.Sqlite`, both version `8.0.22` to align with the current EF/.NET 8 stack.
- **Migration impact:** no entity schema, index, snapshot, or migration changes are planned. No new production package, external integration, or production configuration key is needed. Test dependency additions are part of the implementation approval scope.

## Test plan and acceptance criteria

Use `WebApplicationFactory<Program>` with the real controller metadata, authentication schemes, authorization policies, and limiter middleware. Override bootstrap and cleanup in the test host so tests never connect to configured production databases. Use test-generated JWTs and in-memory collaborator doubles for token-state/proof or target services where the test concerns HTTP access. Do not replace authorization with an always-successful test authentication handler. Test the real proof validator separately and the API-key validator against disposable SQLite relational storage.

1. Administration: all four anonymous/key-only requests return `401`; User returns `403`; Superadmin without valid actor-bound proof receives `400` without target lookup; correctly authorized requests preserve success contracts. Cover expired proof, other actor proof, duplicate proof headers, removed role after token issuance, restricted actor, and revoked JWT session. Assert that denied calls never invoke mutation services.
2. Country: exercise both GETs and every write for no credential, invalid/revoked/expired key, inactive client, no permissions, read-only, write-only, both permissions, User, Superadmin, no role, and custom role. Verify authentication failure is `401` and insufficient access is `403`.
3. Scheme isolation: both headers with valid/invalid combinations; invalid bearer plus valid key is `401`; valid User plus write key cannot write; API-key permission cannot satisfy Superadmin; existing Identity role routes remain bearer-only. Verify only the chosen actor reaches audit/current-user consumers and rate selection. A denial emits one response body, not duplicate challenge JSON.
4. Audit: both routes deny keys and ordinary users and allow Superadmin. Verify service/query not invoked on denial and `no-store` on protected responses.
5. Limits: two clients remain independent; two keys of one client share a quota; human and machine IDs with identical text cannot collide; human Country and administrative allowances are separate; changed configured limit selects a new allowance; malformed stored limit fails closed; exhaust a small quota and verify `429` without sleeps or live traffic. Use fresh test hosts/partitions for unrelated cases.
6. Telemetry: recent timestamp skips update; stale/null timestamp updates; concurrent conditional updates preserve a recent value; tracking timeout/database failure does not fail otherwise valid authentication; actual request cancellation propagates; key-validation database failure never authenticates. Capture logs and assert secret material is absent.
7. Persistence/audit: create client/key, revoke, and deactivate produce allowlisted events in the same transaction; induced audit-insert failure leaves no business mutation; repeated revoke adds no mutation audit; existing key hash/expiry/revocation checks still hold. SQLite verifies relational save behavior, not SQL Server migration/concurrency parity; no migration or live database operation is required.

The current test project contains Account/Authentication/Identity tests in addition to Country tests; some testing guidance is stale. No existing test suite was executed during blueprint preparation. Completion requires the new focused tests and the full solution suite to pass, or an explicitly reported and resolved blocker.

## Implementation order and validation commands

1. Add the test host, access matrix tests, policies, Country scheme selection, and the Identity guard; secure administration and audit first. Keep the final change set together so credential selection, authorization, and quota identity agree.
2. Add transactional administrative audits and response cache controls.
3. Add per-actor Country limiting and bounded best-effort usage tracking, then complete failure-path and persistence coverage.
4. Refresh the targeted documentation and run narrow-to-broad verification. Inspect the final diff for secret material and unrelated changes.

```powershell
dotnet restore .\AhisApiTemplate.sln
dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~ApiKeySecurity"
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

The implementation must place the new tests under the filter's namespace. Restore/build/test may create local outputs and disposable test databases only; no `dotnet ef database update`, production database connection, deployment, commit, or push is authorized by this plan.

## Documentation, rollout, risks, and approval

Update API-client/Country/Audit module guides, their affected nested AGENTS guidance, authentication/authorization and auditing architecture docs, and testing guidance for the new test layers/dependencies. Update root/project dependency guidance for the new test references. Keep the review as a dated historical assessment; append a link to implementation verification only after the fixes are delivered.

Before an independently authorized deployment, inventory existing clients' stored Country permissions and rate limits, human User/Superadmin assignments, and administrative consumers needing step-up. Do not silently grant missing permissions or roles. Clients missing required grants will receive `403`; invalid stored limits will stop key authentication. Coordinate any required data correction separately. Existing key material remains valid subject to its existing eligibility and the newly enforced policies.

Verify a usable Superadmin account and the existing reauthentication flow before rollout. Validate the matrix in an isolated environment, then observe `401`/`403`/`429`, authentication/database errors, and usage-tracking warning counts without logging credentials. Rate limits are explicitly per instance. If the service was publicly exposed while administration was anonymous, separately assess unexpected clients/keys and whether revocation is warranted; this blueprint authorizes no bulk revocation.

Do not use restoration of anonymous administration as rollback. If a release cannot be safely corrected, restrict the affected routes while investigating under separately authorized operations.

**Confidence:** endpoint/policy and persistence findings are source-confirmed; runtime response combinations are to be proven by the planned HTTP tests. **Assumptions:** one application instance for quota guarantees; no tenant-specific access rules; existing Identity role/session invalidation and step-up prerequisites are deployed. **Open business decisions:** none for the confirmed access matrix and proof requirement. Bearer precedence, audit atomicity, tracking tolerance, test dependencies, and the other proposed defaults in this revision are submitted for review with implementation approval.

Per the repository change-blueprint workflow, preparation ends with this document. Implementation changes security and public behavior and requires explicit approval of this saved revision; database mutation and deployment always require separate authorization.
