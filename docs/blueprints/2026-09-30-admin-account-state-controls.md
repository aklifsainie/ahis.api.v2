# Blueprint: administrative disable and enable controls

**Status:** Proposed for approval, with blocking business decisions below. Scope is the two pending administrative account-state routes. Hold placement, hold release, and legacy classification require a separate decision and are not silently bundled into enable.

## Outcome

Add `POST /api/admin/identity/users/{userId}/disable` and `POST /api/admin/identity/users/{userId}/enable` as a coherent pair with one state model, one authorization boundary, and explicit reversal rules. A disabled user must lose existing access and refresh sessions immediately and be denied future authentication until eligible again. Enable must not bypass a hold, unclassified legacy restriction, ordinary lockout, or deleted-account policy.

## Owning module and analogous implementation

**Confirmed:** `AdminIdentityController` dispatches `UnlockAdminUserCommand` and `RevokeAdminUserSessionsCommand` through the custom mediator to `IAccountService`/`AccountService`. The service checks current Superadmin membership and actor-bound step-up proof, changes Identity user/restriction state inside an Identity transaction, and invokes `IIdentityTokenStateService.InvalidateAsync`. `ApplicationUser` has `IsActive`, `IsDeleted`, and `DeletedAt`. `IdentityUserRestriction` records ordinary lockout, administrative/security hold, and unclassified legacy provenance; `IdentityRestrictionService.IsAuthenticationAllowedAsync` fails closed on holds and ambiguous state. The self-service `DeactivateAsync` sets inactive and soft-deleted with no self-service reversal.

## Observed flow and proposed flow

**Observed:** Admin unlock closes only a current ordinary lockout; it cannot lift holds or reactivate/deleted users. No admin disable/enable route or service method exists. Bearer and refresh checks already consume active/deleted state and restriction provenance.

**Proposed:** Add dedicated admin mutation policy/policies in `Program.cs`, controller actions, mediator commands, and narrow Identity service operations. Authenticate bearer Superadmin, recheck current actor role and five-minute actor-bound step-up proof before target lookup, reject unsafe self-action, then make a state transition in an Identity transaction. Invalidate target token/session state on a real change. Attempt redacted actor/target audit and generic user notification after commit; audit or SMTP failure cannot turn a committed transition into an error. Use an explicit outcome enum for changed, no-op, missing target, and refused transition.

## Files to add, modify, or intentionally leave unchanged

- Modify `AdminIdentityController`, `Program.cs`, `IAccountService`, `AccountService`, and the applicable Identity restriction contract/service only if the selected state model needs new provenance operations.
- Add `DisableAdminUserCommand` and `EnableAdminUserCommand` handlers with trusted actor derivation and redacted post-commit audit. Add API, handler, service, and relational transition tests.
- Add an `IdentityContext` migration only if the approved state model needs a distinct durable disable reason/provenance or concurrency field; use `create-ef-migration` and separate approval for generation.
- Leave self-service deactivation/recovery, ordinary unlock, API-client status, and role assignment behavior unchanged unless a reviewed transition rule explicitly requires an adjustment.

## Business behavior and confidence

**Observed invariant:** Inactive or soft-deleted accounts are denied by current bearer/refresh/login checks; holds are separately authoritative and fail closed. **Proposed rule:** Disable means an administrative temporary restriction, distinct from self-service deletion. Enable reverses only that administrative disable and preserves all unrelated restrictions. Repeated disable/enable of the same target state should be idempotent without duplicate mutation audit or notification. A true change rotates security version and revokes all sessions.

**Blocking choice:** Represent administrative disable with `IsActive=false` plus durable administrative provenance, or use an active `AdministrativeSecurityHold` while retaining `IsActive=true`. The existing hold category has no public placement/release workflow and has a one-active-row index, so a hold-based design must define interaction with ordinary lockouts and legacy rows before implementation. Do not implement a plain `IsActive=true` enable that would erase the reason for denial.

## Authorization and security

Dedicated bearer-only Superadmin policy, authenticated-security limiter, current role recheck, actor-bound step-up proof, and self-target denial are the proposed defaults, following admin unlock. Invalid proof must not reveal whether a target exists. Return no credentials, restriction evidence, or sensitive audit values. Define whether a last effective administrator can be disabled; the safe proposed invariant is to reject any transition leaving zero eligible Superadmins, using a serializable transaction and concurrency tests.

## API contract

Both routes accept `X-Step-Up-Proof`; a request body may carry only an allow-listed internal reason code if the owner requires one. Proposed success: `204` for changed or idempotent eligible transitions. Proposed errors: `401`/`403` for authentication/role failure, `400` for invalid proof/unsafe state, `404` for a missing target only after proof validation, `409` when a transition conflicts with a hold/legacy restriction or last-admin invariant, `429` rate limit, generic `500` for pre-commit failure. Final status distinction requires product approval. Responses use `Cache-Control: no-store` and no target state body.

## Entity, persistence, and migration impact

Current `ApplicationUser` and `IdentityUserRestriction` are in `IdentityContext`. A durable, attributable administrative disable should be distinguishable from self-deactivation and ordinary lockout. The chosen encoding determines whether a new nullable field, restriction category, or separate record is necessary. Review existing rows and migration reversibility; do not infer that `IsActive=false` means admin disable in historical data. Migration generation and application are separate approvals.

## Service decision and evidence

Extend `IAccountService`/`AccountService`; Identity managers, restriction provenance, transactions, security version, and notification already live there. No new Application repository or direct controller `IdentityContext` access.

## Transaction, audit, and integration impact

In one Identity transaction: check target and relevant restriction/role state, apply the approved transition, invalidate target token/session state, commit. After commit: one fixed-label actor/target audit and generic email notification where approved. Avoid sending reason text, proof, token values, or restriction evidence through email or audit. If notification is mandatory for success, an outbox/retry design must be separately approved because SMTP cannot join the Identity transaction.

## Test plan

Cover anonymous/non-admin/API-key denial, stale role and proof, self-target, missing target privacy, each state transition, repeated calls, inactive/deleted/locked/held/unclassified targets, last-admin concurrency, rollback on invalidation failure, bearer/refresh invalidation, post-commit audit/email failure, and redaction. Use relational tests for the one-active-restriction constraint and concurrent administrator changes.

## Documentation impact

After implementation, update the two backlog items, Account and security architecture docs, endpoint Swagger remarks, and operational guidance for enable/disable decisions.

## Validation commands

After approval: focused admin state tests, `dotnet restore .\AhisApiTemplate.sln`, `dotnet build .\AhisApiTemplate.sln --no-restore`, `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`; inspect any generated Identity migration. Do not apply migrations during verification.

## Risks, assumptions, and open questions

- **Blocking:** What exact distinction should users/operators see between temporary disable, security hold, self-deactivation, and deletion? Which states may enable reverse?
- **Blocking:** Is a reason code required, and who may place/release a hold or classify a legacy restriction? These are currently internal-only and cannot be treated as ordinary enable.
- **Blocking:** Should disabling the last effective Superadmin be forbidden? Proposed answer is yes, with concurrency proof.
- **Risk:** A state change that commits before email or audit delivery needs a documented retry/escalation policy if those effects are mandatory.
