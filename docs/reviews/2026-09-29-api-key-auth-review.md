# API-key authentication and authorization review

Reviewed: 2026-09-29. This is a source-code review of the current API request path; no HTTP integration test or deployment check was performed. The recommendations below are proposed changes, not approved authorization requirements.

## Request path and existing safeguards

`X-API-Key` is handled by [`ApiKeyAuthenticationHandler`](../../ahis.template.api/ApiClientAuthentication/ApiKeyAuthenticationHandler.cs). It rejects multiple or empty header values, asks [`ApiKeyValidator`](../../ahis.template.infrastructure/ApiClientAuthentication/ApiKeyValidator.cs) to look up the SHA-256 hash, and creates an authenticated `ApiKey` principal containing the client ID, key ID, and the client's `api_permission` claims. The validator rejects unknown, inactive, revoked, or expired keys and inactive clients. Externally, authentication failure uses a generic response; the raw key is generated from 32 random bytes and stored only as a hash ([generator](../../ahis.template.infrastructure/ApiClientAuthentication/ApiKeyGenerator.cs), [client service](../../ahis.template.infrastructure/Services/ApiClientService.cs)).

The [default authentication scheme](../../ahis.template.api/Program.cs#L349) is bearer JWT. Country and audit controllers explicitly accept either bearer or API-key authentication. The dedicated Identity role-management policies in [`Program.cs`](../../ahis.template.api/Program.cs#L133) specify bearer authentication and `Superadmin`; an API key alone does not satisfy those policies. API-client permissions and human Identity roles are separate authorization mechanisms.

## Findings

### 1. Critical — anonymous API-client administration

**Evidence:** [`ApiClientController`](../../ahis.template.api/Controllers/v1/ApiClientController.cs#L12) has a rate-limit attribute but no controller- or action-level `[Authorize]`. Its POST actions create a client and return its raw key, create another key, revoke a key, and deactivate a client (lines 28, 53, 82, and 99). [`ApiClientService`](../../ahis.template.infrastructure/Services/ApiClientService.cs#L25) accepts these calls without checking an actor's role. `createdBy` and similar fields can be null because the controller reads them from the optional principal.

**Impact:** If these routes are reachable, an anonymous caller can mint a usable API key with permissions supplied in the create request, potentially with no expiration, and can disrupt existing clients by revoking keys or deactivating them. The recently added Superadmin RBAC policies protect separate Identity routes; they do not protect `/api/apiclient`.

**Recommended disposition:** Restrict every API-client administration action to a deliberate human bearer policy and server-verified administrative authority. Decide separately whether key creation, revocation, and deactivation require step-up proof and mutation auditing. Add an HTTP-level regression test that an anonymous request cannot create or change a client. Treat this as the first remediation priority.

### 2. High — Country permissions are not enforced

**Evidence:** [`Program.cs`](../../ahis.template.api/Program.cs#L77) defines API-key-only `CountryReadPolicy` and `CountryWritePolicy`, each requiring its respective `api_permission`. [`CountryController`](../../ahis.template.api/Controllers/v1/CountryController.cs#L16) instead uses a controller-wide `[Authorize(AuthenticationSchemes = "Bearer,ApiKey")]`; its read and write actions do not apply either permission policy (GET lines 38 and 80; POST 106; PUT 135; DELETE 159).

**Impact:** Any active API key, including one with no permissions, can read, create, update, and delete Country records. A key granted only `country.read` can also write. For bearer users, these actions similarly require authentication but no role. The permission claims exist, but the endpoint authorization path does not inspect them.

**Recommended disposition:** Define the intended Country access matrix for machine clients and human users, then attach policies that enforce it per action. A policy restricted to the API-key scheme cannot by itself express an `API-key permission OR authorized bearer user` rule; preserve the intended bearer access explicitly. Test keys with no permission, read-only permission, and write permission against each action.

### 3. High — audit history is available to any authenticated client

**Evidence:** [`AuditLogController`](../../ahis.template.api/Controllers/AuditLogController.cs#L15) accepts `Bearer,ApiKey` and has two GET actions (lines 29 and 42), with no audit-specific role or permission policy. Its query path returns audit rows through the mediator.

**Impact:** Any valid API key, regardless of its permission claims, and any valid bearer user can query audit history. The precise sensitivity of individual rows depends on stored audit data, but authorization currently has no distinction between ordinary API access and audit inspection.

**Recommended disposition:** Choose an explicit audit-reading role or permission for each supported identity type; if machine clients have no audit use case, remove the API-key scheme from these endpoints. Add endpoint tests for ordinary bearer users, Superadmins, and keys with and without the chosen permission. Review audit response contents separately for sensitive values.

### 4. Medium — configured client rate limits are not applied

**Evidence:** The create command exposes `RateLimitPerMinute`, and the client service persists it ([command](../../ahis.template.application/Features/ApiKeyAuthenticationFeatures/Commands/CreateApiClientCommand.cs#L33), [service](../../ahis.template.infrastructure/Services/ApiClientService.cs#L52)). [`Program.cs`](../../ahis.template.api/Program.cs#L327) instead registers one named `ApiPolicy` sliding-window limiter with `PermitLimit = 10`, a one-minute window, and a queue of two. Country, audit, and API-client administration use that policy. The limiter does not partition by client or read the stored limit.

**Impact:** The advertised per-client value has no effect. Traffic from one caller can exhaust the shared limiter and affect other callers, including administrators. The code comment says “100 requests,” but the configured limit is 10.

**Recommended disposition:** Decide which endpoints need a client-specific limit, partition by a stable authenticated client identity, and define separate handling for bearer and anonymous traffic. Apply the stored value only after validation and test isolation between two clients. Remove or correct the misleading comment.

### 5. Low — usage tracking can fail an authenticated request

**Evidence:** After successful key validation, [`ApiKeyAuthenticationHandler`](../../ahis.template.api/ApiClientAuthentication/ApiKeyAuthenticationHandler.cs#L79) awaits `UpdateLastUsedAsync` before returning success. [`ApiKeyValidator`](../../ahis.template.infrastructure/ApiClientAuthentication/ApiKeyValidator.cs#L63) performs an EF `ExecuteUpdateAsync` and does not catch a write failure.

**Impact:** A database write error during usage tracking can turn an otherwise valid authenticated request into an error. Each successful API-key authentication also performs a write, adding load and contention at high request rates.

**Recommended disposition:** Decide whether `LastUsedAt` is best-effort telemetry or a hard availability requirement. If best effort, isolate or batch updates while preserving cancellation and avoiding secret logging. Test the chosen behavior when the usage update fails.

## RBAC interaction and proposed verification

The dedicated Identity role-management policies are bearer-only, so the review found no API-key-only bypass into those routes. The practical RBAC collision is uneven coverage: the new role gates protect Identity administration while API-client administration and shared Country/audit actions remain outside equivalent authorization decisions. When both bearer and API-key credentials are supplied to a Country or audit request, those controllers allow either successful scheme; a valid key can therefore authorize the request even if the bearer credential is invalid. That follows from their current two-scheme attributes and should be included in the intended access decision.

Proposed HTTP-level matrix after policy decisions are made:

| Request | Expected property to verify |
| --- | --- |
| Anonymous POST to each API-client administration action | No client or key mutation; no raw key returned. |
| Ordinary bearer user and Superadmin POST to API-client administration | Only the selected administrative authority succeeds. |
| Valid API key with no Country permission on Country GET/POST/PUT/DELETE | Denied according to the selected machine-client policy. |
| Read-only key on Country write; write-capable key on intended actions | Read-only key cannot write; granted operations succeed. |
| Ordinary bearer user and API key on audit GETs | Denied unless explicitly granted audit access. |
| Valid key plus invalid bearer on a dual-scheme endpoint | Matches the explicitly chosen credential-combination behavior. |
| Two API clients with different stored limits | Each is limited independently at the configured rate. |
| Valid key when `LastUsedAt` update fails | Matches the chosen availability/telemetry contract. |

These are proposed tests, not executed results. This review did not change runtime code, policies, database schema, or migrations.
