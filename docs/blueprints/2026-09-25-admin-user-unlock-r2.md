# Blueprint: administrative unlock of an Identity user (revision 2)

**Status:** Implemented in source on 2026-09-25; verification results are recorded with the implementation handoff. This revision supersedes the proposed 2026-09-25-admin-user-unlock.md for implementation of POST /api/admin/identity/users/{userId}/unlock. Migration generation or application, database access, deployment, and external writes remain outside this document.

## Outcome

Give a human Superadmin a narrowly scoped action to end another active user's current ordinary Identity lockout. A committed unlock closes its retained restriction provenance, clears Identity lockout state and failed-attempt count, and invalidates the target's access tokens, refresh tokens, sessions, and step-up proofs in one Identity transaction. It then attempts an actor/target audit and a generic target notification. The action never releases an Administrative/Security Hold or an unclassified legacy restriction and never reactivates or restores an account.

## Owning module and analogous implementation

**Confirmed:** AdminIdentityController owns the administrative route. AccountFeatures uses the repository's custom mediator; its RevokeAdminUserSessionsCommand obtains the actor from ICurrentUserService and calls IAccountService. AccountService uses UserManager, IdentityContext, IdentityUnitOfWork, AccountSecurityProofService, and IdentityTokenStateService. The revoke-sessions endpoint supplies the closest policy, limiter, proof, transaction, result-mapping, and target-scoped audit pattern. The Account module guidance keeps Identity persistence and email coordination in the Identity service boundary.

**Confirmed:** IdentityRestrictionService reads active IdentityUserRestrictions and distinguishes OrdinaryLockout, AdministrativeSecurityHold, and LegacyUnclassified. IdentityContext has a filtered unique index permitting at most one row per user with EndedAtUtc null. AddIdentityUserRestrictions has been generated and source-reviewed, but repository documentation says it has not been applied to a database.

**Revision reason:** The earlier unlock blueprint predates this provenance model and proposes a time-window inference from LockoutEnd. That inference must not be implemented now that restriction provenance is authoritative.

## Observed flow and proposed flow

**Observed:** POST /api/admin/identity/users/{userId}/revoke-sessions dispatches through the mediator. Its service validates the actor's five-minute proof before target lookup, then invalidates target token and session state in an Identity transaction. Its handler attempts a post-commit audit. Bearer, refresh, two-factor, and proof validation now consult restriction provenance. IdentityTokenStateService.InvalidateAsync rotates the security stamp and revokes refresh tokens and sessions.

**Proposed:**

1. The controller accepts route userId and optional X-Step-Up-Proof header, with no request body. It dispatches UnlockAdminUserCommand with HttpContext.RequestAborted. The handler takes the actor ID only from ICurrentUserService and rejects missing actor, blank target ID, missing proof, and every self-target request before mutation.
2. The Identity service verifies the actor's current authoritative Superadmin membership and validates the actor-bound proof before target lookup. An invalid, expired, stale, or wrong-actor proof receives one generic validation response. API-client credentials cannot satisfy the bearer policy or this actor check.
3. After those checks, begin an Identity transaction and read the target and its active provenance afresh. Capture one UTC time for eligibility. Unknown target produces a generic not-found outcome. Inactive, soft-deleted, or lockout-disabled targets are ineligible.
4. A clean already-unlocked target has LockoutEnd null and no active restriction row. Return idempotent success without changing failed-attempt count, token state, provenance, audit, or notification. An active expired ordinary row, a non-null legacy LockoutEnd without provenance, and any inconsistent combination are ineligible rather than clean idempotency.
5. An unlockable target must have exactly one active OrdinaryLockout row with ExpiresAtUtc after the captured time and a future Identity LockoutEnd matching that row's recorded expiry at database precision. AdministrativeSecurityHold and LegacyUnclassified are ineligible regardless of LockoutEnd. Do not infer category from lockout duration or a sentinel date.
6. Conditionally close only that row while it is still active, ordinary, and unexpired, recording EndedAtUtc and EndedByUserId while retaining its original category, origin, and history. Require exactly one updated row. Clear LockoutEnd and reset AccessFailedCount through UserManager operations, preserving LockoutEnabled. Then call IIdentityTokenStateService.InvalidateAsync and commit. Identity concurrency failure, changed provenance, failed Identity operation, or failed invalidation rolls the transaction back; no partial unlock may succeed.
7. Record confirmed commit before any external effect. Attempt one generic notification and one fixed-label target-scoped audit after commit using cancellation independent of a cancelled request. Suppress and safely log delivery failures without changing the committed HTTP result. Send neither effect for clean idempotency.

## Files to add, modify, or intentionally leave unchanged

- Add the new action to ahis.template.api/Controllers/v1/AdminIdentityController.cs and its dedicated bearer-only policy in ahis.template.api/Program.cs. Reuse AuthenticatedSecurityPolicy without changing its five-per-minute, per-actor configuration.
- Add ahis.template.application/Features/AccountFeatures/Commands/UnlockAdminUserCommand.cs. Keep actor derivation, generic result mapping, and the post-commit explicit audit in the handler.
- Add a narrow outcome-returning method to ahis.template.identity/Interfaces/IAccountService.cs and implement proof and current-role verification, transactional unlock, and post-commit notification in ahis.template.identity/Services/AccountService.cs. Use IdentityRestrictionService/IdentityContext for provenance; do not duplicate a time-window classifier. If the restriction service needs a narrow conditional-close method, add it to its interface and implementation within Identity.
- Add focused AccountFeature handler and service tests. Add relational and authenticated API coverage where needed for the transaction, persistence, and policy guarantees.
- After implementation verification, update docs/backlog/identity-security-endpoints.md, docs/modules/account.md, and docs/architecture/authentication-authorization.md with the shipped behavior and actual validation evidence.
- Leave ApplicationUser, IdentityContext schema, migrations, role bootstrap, production settings, API-client permissions, password/MFA state, other administrative actions, and unrelated modules unchanged. If implementation reveals a required schema or role-store change, stop and revise the blueprint.

## Business behavior and confidence

**Owner-resolved behavior carried from the first blueprint:** Global Superadmin target scope; no self-unlock; an active ordinary lockout only; clean already-unlocked idempotency; generic target notification after actual unlock; no target notification for idempotency; and no target security-state response.

**Confirmed source invariant:** An active AdministrativeSecurityHold or LegacyUnclassified row remains blocking without relying on LockoutEnd. An active ordinary row expires according to ExpiresAtUtc. A missing active row plus non-null LockoutEnd is treated as an ambiguous legacy mismatch by authentication.

**Inference adopted for safety:** An ordinary provenance row and Identity LockoutEnd must agree for an administrative unlock. Any mismatch is ineligible and requires investigation; the endpoint does not repair it.

## Authorization and security

Register IdentityAdminUserUnlockPolicy with the JWT bearer authentication scheme, authenticated user requirement, and IdentityRoleNames.Superadmin. Apply AuthenticatedSecurityPolicy to the action. The service also checks current Superadmin membership before target lookup, because a role claim in an already-issued bearer does not by itself prove that membership still exists. A current actor-bound five-minute X-Step-Up-Proof is required. Every self-action is rejected, including a clean idempotent request.

Do not log or return bearer or proof values, refresh cookies, security stamps, token hashes, credentials, target email address, internal restriction reason or evidence, or unmasked audit values. Actor and target IDs and fixed action labels may be recorded. A target unlock does not clear the actor's refresh cookie.

## API contract

- POST /api/admin/identity/users/{userId}/unlock; route ID and X-Step-Up-Proof header only; no request body.
- 204 No Content with Cache-Control: no-store for a committed unlock or clean idempotent request; empty body.
- 400 Bad Request with a generic validation response for missing/invalid proof, blank route ID, self-targeting, or an existing ineligible target. Do not disclose its restriction category or account state.
- 404 Not Found with a generic response for an unknown target, reachable only after authorization, current-role check, and valid actor proof.
- 401 Unauthorized for missing/invalid bearer; 403 Forbidden for a principal that fails the policy, including an API-client principal; 429 Too Many Requests from the existing limiter; generic 500 ProblemDetails for operational failure before confirmed commit.

Set Cache-Control: no-store for action responses. Add accurate XML/Swagger summary, proof-header remarks, and response metadata. Use the neighboring administrative mutation's controller mapping conventions without exposing exception details.

## Entity, persistence, and migration impact

Use existing IdentityUsers, IdentityUserRestrictions, RefreshTokens, and RefreshSessions data in IdentityContext. Closing a restriction sets EndedAtUtc and EndedByUserId without deleting or changing its original provenance. The conditional close, Identity lockout-field updates, security-stamp rotation, and token/session revocations share one IdentityUnitOfWork transaction. The restriction update must check affected-row count; UserManager's concurrency result must also be checked. A race fails safely.

No endpoint-specific entity, new table, migration, dependency, or production configuration is planned. The existing AddIdentityUserRestrictions migration and its environment rollout are prerequisites, not work authorized by this blueprint.

## Service decision and evidence

Extend IAccountService narrowly. The analogous administrative session mutation already delegates Identity manager operations, proof validation, transaction handling, and token invalidation to AccountService. IdentityRestrictionService owns provenance queries and any conditional provenance write. An Application repository would cross the demonstrated Identity boundary.

## Transaction, audit, and integration impact

IdentityUnitOfWork owns the database transaction. Roll back on any pre-commit failure, including a changed restriction row or failed InvalidateAsync; distinguish confirmed commit from post-commit failure. The explicit IAuditLogger event uses AuditActionEnum.StatusChange, entity AccountSecurity, target ID as entityId, and fixed label AdminUserUnlocked. AuditLogger writes through the separate ApplicationDbContext and is best effort; the retained restriction row is the authoritative closure record. Notify the target through the existing email sender pattern with generic security-change text only after commit. Email and audit are not in the Identity transaction.

## Test plan

- Handler/API: bearer and Superadmin policy, current-role check, rate limit, actor provenance, self-action rejection, proof-before-target ordering, generic errors, unknown-target 404 only after proof, no-store empty 204, no sensitive response or audit payload, and no audit for clean idempotency.
- Identity service: missing/expired/stale/wrong-actor proof; ordinary active and boundary-expired rows; hold and unclassified rows with null or expired LockoutEnd; missing provenance with non-null LockoutEnd; expiry mismatch; inactive/deleted/lockout-disabled target; clean idempotency without failed-count or token changes; and generic notification only after actual commit.
- Relational: one conditional row closure with actor/time, retained history, racing target or restriction change, rollback of provenance/lockout/token/session writes on pre-commit failure, and rejection of previously valid target bearer, refresh session, and step-up proof after commit. Verify audit or email failure after commit preserves success. Mock-only tests do not prove transaction atomicity.

After implementation approval, run focused unlock tests, then dotnet restore, dotnet build --no-restore, and dotnet test --no-build --no-restore for AhisApiTemplate.sln. Do not use dotnet ef database update as routine verification.

## Documentation impact

Mark the backlog item complete only after implementation and verification. Update the Account module and authentication/authorization guides to describe provenance-based eligibility, the Superadmin policy, proof requirement, invalidation, notification, audit, and deployment prerequisites. Preserve the first blueprint as historical context; this revision is the implementation source of truth.

## Risks, assumptions, and open questions

**Deployment prerequisites:** Repository documentation says the restriction migration is generated but not applied. The provenance blueprint requires environment-specific migration inspection, legacy classification, writer reconciliation, and enforcement validation before dependent unlock is enabled. These facts cannot be proved from source alone. A non-transactional or bypass lockout writer would break the required provenance guarantee and blocks enablement until repaired under its own approved scope.

**Role prerequisite:** Program.cs configures AddRoles<IdentityRole>, while IdentityContext explicitly maps IdentityRole<string> to IdentityRoles. The separate role-store-alignment blueprint documents the mismatch. Current Superadmin membership, role storage, and fresh bearer issuance must be verified in the target environment; this endpoint does not repair or seed roles.

**Implementation risk:** Existing IdentityUnitOfWork and neighboring AccountService patterns place commit and later effects close to broad exception handling. Implement a clear confirmed-commit boundary, and test cancellation or failure after commit so an already completed unlock is not reported as failed. Recheck the target and restriction inside the transaction and use a conditional provenance close to avoid closing a changed hold or classification.

**Assumptions:** Identity user IDs remain opaque strings. The existing proof, limiter, Identity token-state service, email sender, and audit logger are reused. The owner decisions carried from the prior blueprint remain valid. No endpoint business-policy question is open; environment rollout evidence remains a prerequisite.
