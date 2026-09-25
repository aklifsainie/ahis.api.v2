# Blueprint: administrative user security-state endpoint

**Status:** Draft — approval required before implementation.

## Outcome

Add a read-only administrative endpoint, `GET /api/admin/identity/users/{userId}/security-state`, that lets an explicitly authorized human administrator view a deliberately limited security-state projection for a target Identity user. The endpoint must preserve the current Identity service boundary, produce actor/target audit evidence, and disclose no credential, token, secret, or unmasked sensitive audit material.

This blueprint covers only the first P2 administrative Identity-control endpoint. It does not authorize the other administrative controls, role management, user-role assignment, schema work, migration generation/application, deployment, or policy configuration outside the code required for this endpoint.

## Owning module and analogous implementation

**Confirmed ownership:**

- HTTP action: `ahis.template.api`.
- Request, handler, response mapping, and explicit read audit: `ahis.template.application`.
- Identity-user lookup, Identity credential-state reads, refresh-session count, and Identity-context access: `ahis.template.identity` behind a service interface.
- Public response model: `ahis.template.domain` following the repository convention for Account public view models.

**Confirmed analogous flow:** `GET /api/account/security-summary` in `AccountController` dispatches `GetSecuritySummaryQuery`; its handler derives the caller from `ICurrentUserService`, calls `IAccountService.GetSecuritySummaryAsync`, maps a response, and records an `AccountSecurity` view audit. The target endpoint must retain the mediator/service/audit shape, but must use the route target user ID only after the controller authorization policy succeeds.

**Observed difference:** the current self-service summary has no client-supplied user selector. This endpoint intentionally needs a target `{userId}` and therefore requires a dedicated administrative authorization decision and actor/target audit treatment.

## Observed flow and proposed flow

### Confirmed current flow

1. Bearer validation rejects invalid issuer/audience/lifetime/signature, inactive/deleted/locked-out users, stale security versions, and inactive sessions.
2. `GET /api/account/security-summary` is authenticated, uses `AuthenticatedSecurityPolicy` rate limiting, and returns confirmation, credential-presence, MFA, recovery-code-count, and active-session-count state for the caller only.
3. `IAccountService.GetSecuritySummaryAsync` already owns the necessary Identity-manager and `IdentityContext` style of reads for the self-service projection.
4. No dedicated human-administrator authorization policy currently exists in `Program.cs`; existing Country policies are API-key policies and must not be reused for human Identity administration.
5. `IAuditLogger.LogAsync` derives the current actor from `ICurrentUserService` unless an override is explicitly required; it captures request context separately.

### Proposed flow

1. The API action receives a route-constrained non-empty target `userId`, requires a new dedicated *human administrator security-read* policy, and applies an explicit administrative read rate-limit policy or an approved reuse of an existing authenticated policy.
2. The action dispatches `GetAdminUserSecurityStateQuery` through the custom `IMediator`, passing only the target user ID and `HttpContext.RequestAborted`.
3. The handler verifies the target ID is valid, calls a dedicated Identity-bound administrative read service, maps its DTO to a minimal public response model, and writes one explicit `View` audit event that records the actor through the normal audit context and the target as the audit entity ID. Audit metadata must be a fixed operation label only, not a serialized response.
4. The Identity service looks up the target through `UserManager<ApplicationUser>` and obtains only approved state: account active/deleted and lockout state; email/phone confirmation; password-present; MFA-enabled/authenticator-configured; remaining recovery-code count; and active-session count. It must not project or log security stamps, password hashes, authenticator keys or URIs, recovery-code values, refresh-token values/hashes, reset/challenge tokens, signing data, or sensitive audit contents.
5. A target that does not exist returns the approved, documented not-found response. The endpoint must not blur missing targets with an authorization failure after the policy has admitted the actor; target existence is a legitimate administrative read result. Unauthorized callers must receive the framework 401/403 behavior and no target data.

## Files to add / modify / intentionally leave unchanged

### Add

- `ahis.template.api/Controllers/v1/AdminIdentityController.cs` — route/action, policy/rate-limit attributes, Swagger response metadata, mediator dispatch only.
- `ahis.template.application/Features/AccountFeatures/Queries/GetAdminUserSecurityStateQuery.cs` (or an approved new `AdminIdentityFeatures` feature folder) — custom mediator request and handler.
- `ahis.template.domain/Models/ViewModels/AccountVM/AdminUserSecurityStateResponseVM.cs` (or an approved administrative Identity VM location) — minimal public response projection.
- `ahis.template.identity/Models/DTOs/AdminUserSecurityStateDto.cs` — internal service DTO, if the existing self-service DTO cannot be reused without conflating self-service and administrative contracts.
- Focused unit tests for the query handler; controller/policy integration tests when an authenticated administrative test host is available.

### Modify

- `ahis.template.api/Program.cs` — add a dedicated human-administrator security-read authorization policy and the approved rate-limit policy only after the role/claim source and policy name are approved.
- `ahis.template.identity/Interfaces/IAccountService.cs` **or** a new narrow Identity administrative service interface — expose the administrative read operation.
- `ahis.template.identity/Services/AccountService.cs` **or** the corresponding new administrative service — perform target lookup and minimal state projection.
- Dependency registration only if a new service interface/class is selected.
- `docs/backlog/identity-security-endpoints.md` — update the endpoint's status/evidence only after implementation, tests, and approval criteria are completed; also correct its stale progress-table entries only as a separate documentation decision.
- Relevant module/API documentation and Swagger remarks after the final contract is approved.

### Intentionally leave unchanged

- `ApplicationUser`, `RefreshSession`, `RefreshToken`, Identity schema/configuration, migrations, token issuance/validation, authentication flows, email delivery, and refresh-cookie behavior.
- API-key policies and API-client permissions: they are a separate principal and claim model.
- The other six P2 administrative Identity-control endpoints and all P2 role-management work.

## Business behavior and confidence

**Confirmed:** `ApplicationUser` has `IsActive`, `IsDeleted`, `LockoutEnd`, Identity confirmation flags, 2FA state, and security-related custom fields. Existing self-service summary demonstrates safe access to credential-presence, MFA state, remaining recovery-code count, and active-session count.

**Proposed behavior:** return a read-only state projection for a known target. This operation must not alter the target account, tokens, sessions, security stamp, recovery codes, MFA configuration, role memberships, audit history, or notification state.

**Open contract decision:** the product owner must confirm the exact state fields. The proposed initial allow-list is:

- target user ID;
- active, soft-deleted, and locked-out state (with lockout end only if an owner approves its exposure);
- email-confirmed and phone-confirmed flags;
- password-present, MFA-enabled, and authenticator-configured flags;
- remaining recovery-code count and active-session count.

Do not include the target's email address, username, names, timestamps, failure counts, roles, claims, security-stamp version, raw lockout internals, or other profile data unless a future approved contract adds them.

## Authorization and security

This is a public API, authorization, and security behavior change. Explicit approval of this exact blueprint revision is required before implementation.

- Add a dedicated policy such as `IdentityAdminSecurityReadPolicy`; the final policy name and its role/claim requirement are open decisions. It must use the human bearer-authentication path, require an authenticated user, and require a server-controlled administrator authorization signal.
- Do not use `CountryReadPolicy`, `CountryWritePolicy`, API-client permissions, or a bare `[Authorize]` as a substitute.
- The policy must be applied directly to the action/controller and covered by integration tests proving no authenticated non-administrator can read a target.
- The endpoint is read-only; step-up authentication is not proposed by default. Owner approval must decide whether security-state reads are sensitive enough to require a current step-up proof.
- Apply a deliberate rate-limit decision. The existing `AuthenticatedSecurityPolicy` is five requests per minute partitioned by authenticated user; reuse is possible but must be consciously approved for administrative read traffic or replaced by an admin-specific policy.
- Validate the route ID and use it only as a target selector. Never derive the actor from that route value.
- Return 401/403 without target data when authentication/authorization fails. Do not return secret data in any success or error response. Add no raw request/response body logging.

## API contract

**Proposed route:** `GET /api/admin/identity/users/{userId}/security-state`

**Request:** no body; one required route parameter, `userId`, in the repository's canonical ASP.NET Core Identity user-ID format. A route constraint/validation rule must be selected after inspecting the actual `ApplicationUser.Id` format; do not assume GUID IDs because `IdentityUser` uses strings.

**Proposed success:** `200 OK` with the approved allow-listed administrative security-state response. No token, key, credential, recovery-code, authenticator, or audit payload is returned. The response should be `Cache-Control: no-store` unless the owner explicitly approves caching of security state.

**Proposed failure behavior:**

- `400 Bad Request` for malformed/blank target ID where model/route validation identifies it.
- `401 Unauthorized` for no/invalid bearer authentication.
- `403 Forbidden` for an authenticated caller that lacks the dedicated policy.
- `404 Not Found` for an authorized caller where the target user is absent, subject to owner confirmation of the administrative error-disclosure rule.
- `429 Too Many Requests` according to the approved rate-limit policy.
- `500 Internal Server Error` only for operational failures, with no sensitive detail.

Swagger must declare authentication, required authorization policy, target ID semantics, response fields, no-store behavior if adopted, and each actual status code.

## Entity, persistence, and migration impact

**Confirmed:** the required data already exists in ASP.NET Core Identity and the existing `IdentityContext` session model.

**Decision:** no entity change, schema change, EF configuration change, or migration is planned. The implementation must use no-tracking read semantics where practical and must not write Identity state. Do not generate or apply a migration as part of this endpoint.

## Service decision and evidence

**Decision proposed for approval:** retain Identity reads behind an Identity service, not an application repository. Either extend `IAccountService` with a narrowly named administrative read method or add a dedicated narrow administrative Identity service if the implementation would otherwise make `IAccountService` misleadingly broad.

**Evidence:** root and Account-feature guidance place `UserManager`, Identity persistence, and account-security workflows in the Identity service boundary. `GetSecuritySummaryQueryHandler` already delegates security-state retrieval to `IAccountService`; an application repository that wraps `UserManager` or `IdentityContext` would violate the documented ownership boundary.

**Open design choice:** choose between extending `IAccountService` (least new DI surface; close to existing summary) and a dedicated `IAdminIdentityService` (clearer separation for the seven planned admin controls). Decide this before coding and keep the selected interface minimal.

## Transaction, audit, and integration impact

- No transaction is planned because the endpoint is read-only. If the underlying identity/session query needs multiple reads, it must return a coherent best-effort snapshot and document any unavoidable race between counts and flags.
- Add one explicit `IAuditLogger` `View` event after a successful response projection. Record actor through normal current-user context and target user ID as `entityId`; fixed metadata may identify `AdminSecurityStateViewed`. Do not serialize state, IDs beyond the target entity ID, credentials, session identifiers, tokens, keys, email addresses, or audit values into metadata.
- No email, notifications, external API, background job, cache invalidation, cookie change, or configuration secret is planned.

## Test plan

### Focused handler/service tests

- Authorized handler path maps every approved allow-listed field from the Identity service DTO.
- Handler passes the route target ID, not the current actor ID, to the service.
- Empty/malformed target ID fails before service/audit invocation according to the final validation design.
- Missing target result maps to the approved not-found/domain result without target state.
- Service does not return forbidden fields; add a response-shape regression test covering secrets/tokens/keys/password hashes/recovery values/security stamps.
- Successful view creates exactly one audit record with the actor in audit context, the target as entity ID, and non-sensitive fixed metadata.
- Failed lookup and failed projection paths follow the approved audit policy without exposing data.

### Authorization/controller/integration tests

- Anonymous request returns 401.
- Authenticated non-administrator returns 403 and cannot infer target state.
- Authorized administrator receives 200 only for an existing target and exact approved response shape.
- Missing target returns the approved 404 behavior only to an authorized administrator.
- Invalid target returns the approved validation response.
- Rate-limit behavior returns 429 when its limit is exceeded.
- Swagger/API metadata exposes only the intended contract.

Use an Identity-backed integration test for actual policy and `UserManager`/session-query behavior if the current test environment can host the required Identity stores. Unit tests alone cannot prove policy attachment or relational query semantics.

## Documentation impact

- Update `docs/backlog/identity-security-endpoints.md` with completion date, controller/service/tests, and this approved blueprint after implementation is verified.
- Update the Account or a new administrative Identity module guide to state the controller/service/policy ownership and response redaction boundary.
- Update API Swagger remarks and any endpoint reference.
- Do not treat the existing backlog as approval; it records proposals and source observations only.

## Validation commands

Run narrow to broad after approval and implementation:

```powershell
dotnet test .\ahis.template.test\ahis.template.test.csproj --filter "FullyQualifiedName~AdminUserSecurityState"
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

Also run the planned authenticated integration/policy tests when available. Do not run `dotnet ef database update`; no migration is planned.

## Risks, assumptions, and open questions

- **High-risk approval gate:** policy design, role/claim source, exposed state fields, status-code disclosure, step-up requirement, and rate-limit choice all affect authorization/security behavior and need owner approval.
- **Observed risk:** the repository has Identity role storage and JWT role claims but no confirmed role-management API or dedicated administrative policy. The initial administrator bootstrap mechanism is not defined in the backlog. This endpoint must not invent a bootstrap path or rely on an unverified role name.
- **Assumption:** an authorized human administrator may learn the target account's limited security state. If support or tenancy boundaries apply, they must be defined before implementation.
- **Open question:** should target email/username be included for operator usability? The default in this blueprint is no; user lookup/identity discovery is outside this endpoint.
- **Open question:** should lockout end timestamp, recovery-code count, and active-session count be exposed, rounded, or omitted? Counts can reveal security posture and need product approval.
- **Open question:** must an administrator provide step-up proof to read another user's security state, and what proof/expiry policy would apply?
- **Open question:** should a successful read be audited synchronously as current `IAuditLogger` behavior does, or may audit failure block the response? Follow the existing audit contract unless its failure semantics are separately approved.
- **No migration/deployment assumption:** this endpoint is designed against the current source model. Existing unapplied Identity migrations remain a separate deployment concern and must not be applied as part of this work.
