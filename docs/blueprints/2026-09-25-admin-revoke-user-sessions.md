# Blueprint: administrative revocation of a user's sessions

**Status:** Proposed for approval. This blueprint covers only `POST /api/admin/identity/users/{userId}/revoke-sessions`. Approval applies to the saved file revision.

## Outcome and confirmed business decisions

Add a Superadmin-only action that invalidates a target Identity user's access tokens, step-up proofs, refresh tokens, and refresh sessions. The decisions for this endpoint are:

1. Authorize the existing `Superadmin` role. Human-administrator provisioning and the observed role-store alignment issue are separate prerequisites, outside this endpoint.
2. Require an actor-bound step-up proof valid for five minutes.
3. Permit self-targeting and clear the actor's refresh cookie. After a successful commit, the actor's current token and proof are invalid.
4. For a fully authorized and stepped-up actor, a missing target returns `404 Not Found`.
5. Inactive, deleted, and locked existing targets remain revocable. Return `204 No Content` even when no active sessions exist.
6. Audit is best effort. Audit failure must not turn an already-committed revocation into a `500` response.
7. Send no target notification.

## Owning module and source evidence

The API controller is `ahis.template.api/Controllers/v1/AdminIdentityController.cs`. Account feature commands use the repository's custom mediator in `ahis.template.application/Features/AccountFeatures/Commands/AccountSecurityCommands.cs`; Identity workflows remain behind `IAccountService` in `ahis.template.identity/Interfaces/IAccountService.cs` and `ahis.template.identity/Services/AccountService.cs`.

The closest mutation is `POST /api/account/sessions/revoke-all`: its controller dispatches a command, the service validates a step-up proof, opens an Identity transaction, calls `IIdentityTokenStateService.InvalidateAsync`, commits, and the controller clears the refresh cookie. `InvalidateAsync` rotates the target security stamp and revokes active `RefreshTokens` and `RefreshSessions` in `IdentityContext`. Bearer validation in `ahis.template.api/Program.cs` checks the security version and active session. `AccountSecurityProofService` issues five-minute proofs bound to a user ID and current security version and validates expiry with zero clock skew. The existing administrative read action uses a `Superadmin` policy; its service lookup accepts inactive, deleted, and locked targets.

## Proposed route-to-response flow

1. Add an action to `AdminIdentityController` at the exact route above. Require a dedicated bearer-authenticated `Superadmin` mutation policy and the existing `AuthenticatedSecurityPolicy` rate limit. Accept the actor's proof through `X-Step-Up-Proof`, following the existing single-session revocation header convention. No request body is needed.
2. Dispatch a new `RevokeAdminUserSessionsCommand` through `IMediator`, with the opaque string route `userId`, proof, and `HttpContext.RequestAborted`. Derive the actor ID only from `ICurrentUserService`; never treat the route ID as the actor.
3. In the Identity service, validate the actor-bound proof **before** looking up the target. The framework has already authenticated and authorized the actor. A missing or invalid proof returns a generic validation failure without disclosing target existence. Do not validate the proof against the target ID.
4. Look up the target by `UserManager<ApplicationUser>.FindByIdAsync`. A missing target maps through an application `EntityNotFoundError` to `404`. Do not reject an existing target because it is inactive, soft-deleted, locked, or has zero active sessions.
5. Begin an `IdentityContext` transaction, call the existing token-state invalidation for the target, and commit only after its security-stamp update and token/session updates succeed. On a known pre-commit failure, roll back and return an operational `500` without sensitive detail. Do not send email or another notification. Keep post-commit work outside the revocation failure path.
6. After commit, attempt one explicit audit event. Suppress and safely log audit-delivery failure, including cancellation after commit, so the response remains successful. For self-targeting, clear the canonical refresh cookie with `RefreshCookie.Clear(Response)`; return `204 No Content`. A different-target request does not clear the actor's cookie.

## Files to add or modify

- `ahis.template.api/Controllers/v1/AdminIdentityController.cs`: action, policy and rate-limit attributes, proof-header binding, response mapping, self-target cookie clearing, and Swagger metadata.
- `ahis.template.api/Program.cs`: dedicated administrative session-revocation policy requiring the bearer scheme, authenticated user, and `IdentityRoleNames.Superadmin`. Reuse the existing five-per-minute authenticated security rate limit; do not change its configuration.
- `ahis.template.application/Features/AccountFeatures/Commands/RevokeAdminUserSessionsCommand.cs` (new): request and handler, actor provenance, target-not-found mapping, and post-commit best-effort audit.
- `ahis.template.identity/Interfaces/IAccountService.cs` and `ahis.template.identity/Services/AccountService.cs`: narrow actor-proof/target-revocation operation. Keep `UserManager`, proof validation, Identity transaction, and token-state invalidation in Identity. A `Result<bool>` can distinguish an existing target (`true`) from a missing target (`false`); failures represent operational errors. The handler maps `false` to `EntityNotFoundError`.
- `ahis.template.test/TestFeatures/AccountFeature/RevokeAdminUserSessionsCommandHandlerTest.cs` (new) and focused Identity/API integration tests where the test host supports the stores and authentication pipeline.
- After implementation is verified: `docs/backlog/identity-security-endpoints.md`, `docs/modules/account.md`, and `docs/architecture/authentication-authorization.md`; update Swagger remarks on the action.

Leave the other P2 administrative endpoints, role management, API-key permissions, target account status, credential/MFA state, and notification flows unchanged.

## Authorization, API contract, and redaction

`401` applies to absent or invalid bearer credentials; `403` applies to an authenticated caller without `Superadmin`. Neither response discloses target existence. Reject blank route IDs or missing/invalid/expired/wrong-actor proofs with a generic `400` validation response after authorization; inspect the actual route/model binding when implementing to document the precise blank-ID behavior. Only a fully authorized actor with a valid proof can receive `404` for a missing target. Return `204` for any existing target after a successful commit, including a repeated revocation. Return `429` under `AuthenticatedSecurityPolicy`. Reserve `500` for failures before a confirmed commit. Do not return target data or a response body on success.

The audit should record the current actor through `IAuditLogger` context and the target user ID as `entityId`, with a fixed operation label such as `AdminSessionsRevoked` and an appropriate security action enum. Do not put the proof, bearer token, refresh cookie, security stamp, token hashes, credentials, target profile data, or unmasked audit values in audit metadata, responses, or logs. `AuditLogger` writes to `ApplicationDbContext` separately from the Identity transaction and currently suppresses non-cancellation persistence exceptions; the new handler must also prevent any post-commit audit exception from changing the HTTP result. Audit delivery is attempted, not guaranteed.

## Persistence, migration, and integration impact

Use the existing `IdentityContext` user, hashed refresh-token, and refresh-session model. The existing `IdentityTokenStateService.InvalidateAsync` supplies the required account-wide invalidation; ensure the Identity transaction encloses its security-stamp and bulk token/session updates. No new entity, schema, migration, dependency, session store, configuration secret, external integration, or notification is planned. Do not generate or apply a migration under this blueprint.

The separate role-store alignment blueprint, `docs/blueprints/2026-09-25-identity-role-store-alignment.md`, records the mismatch between runtime `IdentityRole`/`AspNetRoles` and an explicit `IdentityRole<string>`/`IdentityRoles` mapping. Resolving that mismatch and provisioning a human `Superadmin` are deployment prerequisites for usable access, not changes authorized here. This endpoint must not invent role bootstrap behavior or mutate role data.

## Tests and validation after approval

- Handler: actor ID comes from the authenticated principal, target ID from the route; proof is passed for actor validation; missing actor/proof fails without target lookup; missing target maps to `404`; success audits actor and target with fixed metadata; audit failure after commit still produces success.
- Identity service with a relational test store: proof for another actor, expired proof, and stale proof fail before target lookup; inactive/deleted/locked targets are revocable; zero-session and repeated calls succeed; security-stamp rotation and refresh token/session revocation commit together; a forced pre-commit failure rolls back; self-targeting invalidates the actor's bearer and proof.
- API integration: anonymous `401`, authenticated non-Superadmin `403`, API key denied, fully authorized missing target `404`, invalid proof `400`, existing target `204`, rate-limit `429`, self-target refresh-cookie clearing, different-target cookie retention, and accurate Swagger metadata. Verify sensitive values are absent from responses and audits.
- Run focused tests first, then concise solution validation after implementation: `dotnet test .\ahis.template.test\ahis.template.test.csproj --filter "FullyQualifiedName~RevokeAdminUserSessions" --verbosity quiet`; `dotnet restore .\AhisApiTemplate.sln --verbosity quiet`; `dotnet build .\AhisApiTemplate.sln --no-restore --verbosity quiet`; `dotnet test .\AhisApiTemplate.sln --no-build --no-restore --verbosity quiet`. Do not run a database update. None of these commands is part of this planning task.

## Risks, assumptions, and open decisions

**Risk:** The self-service revoke-all service currently performs notification after commit inside a broad catch. The administrative path must omit notification and must not classify a known committed revocation as failed because later audit work throws.

**Risk:** Role-store alignment and administrator provisioning must be resolved separately before a real human can exercise the policy. The existing `Superadmin` name is confirmed in source; no bootstrap or tenant rule is inferred.

**Non-blocking assumptions:** Keep Identity user IDs as opaque strings; use the existing `X-Step-Up-Proof` header and five-per-minute authenticated security rate limit; use a dedicated mutation policy rather than broadening the read policy. These choices can be implemented without changing the seven approved business decisions. No tenant boundary is present in the inspected endpoint or service path, so this blueprint adds none. There are no remaining blocking business questions.