# Blueprint: administrative MFA reset and required password reset

**Status:** Proposed for approval, subject to the recovery and notification decisions below. Scope is `POST /api/admin/identity/users/{userId}/reset-mfa` and `POST /api/admin/identity/users/{userId}/require-password-reset`.

## Outcome

Give authorized support administrators two explicit credential interventions while preserving account ownership and recovery assurance. Both operations invalidate current access and refresh sessions. Neither returns an authenticator key, recovery code, reset token, or password to the administrator. A forced password reset is an enforceable future-login gate, not merely a notification or an untracked security-stamp rotation.

## Owning module and analogous implementation

**Confirmed:** `AdminIdentityController` → custom mediator → `IAccountService`/`AccountService` is the existing administrative security path. Self-service `ResetAuthenticatorAsync` disables MFA, resets the key and recovery codes, updates `ApplicationUser`, calls `IIdentityTokenStateService.InvalidateAsync`, and sends a generic security email. `AuthenticationService` coordinates password login, recovery, 2FA challenge, and refresh. `ApplicationUser` has no explicit password-reset-required flag or timestamp. `AccountRecoveryChallenge` stores a hash-only, single-use, short-lived recovery challenge, and account recovery requires MFA evidence for MFA-enabled users. Login and refresh enforce active/deleted/restriction state and security version.

## Observed flow and proposed flow

**Observed:** An administrator can revoke sessions or unlock an ordinary lockout, but cannot reset another user's MFA or impose a future password-reset obligation. Self-service MFA reset assumes the account owner has completed step-up. Password reset currently changes credentials through owner recovery, without an administrative force-reset gate.

**Proposed:** Add two admin actions with dedicated bearer-only Superadmin policy/policies, authenticated-security limiter, current actor-role recheck and actor-bound step-up proof before target lookup. Use separate mediator commands so each action has its own transition tests and audit label; keep Identity manager and persistence work in the Identity service. A successful MFA reset clears authenticator and recovery-code state using the validated self-service pattern, invalidates sessions, and requires the normal enrollment flow for future MFA. A successful require-password-reset marks durable state, invalidates sessions, and blocks password/passkey login and refresh until an approved recovery/set-password flow satisfies it. Do not send an administrator-created reset token in an API response.

## Files to add, modify, or intentionally leave unchanged

- Modify `AdminIdentityController`, `Program.cs`, `IAccountService`, `AccountService`, `AuthenticationService`, and the relevant recovery/password-completion path.
- Add `ResetAdminUserMfaCommand` and `RequireAdminUserPasswordResetCommand` handlers; add a durable reset-required field/entity and `IdentityContext` mapping only after its semantics are approved. Generate the Identity migration via `create-ef-migration` after separate approval.
- Add handler, Identity service, HTTP policy, relational transition, and recovery-bypass tests. Update Account/Authentication module docs, security architecture, Swagger remarks, and backlog upon completion.
- Leave API-client keys, direct role claims, self-service reset semantics, and existing session identifiers unchanged except where the approved forced-reset gate must apply.

## Business behavior and confidence

**Confirmed invariant:** Existing MFA recovery requires approved evidence and current security state; an admin cannot safely bypass it merely by invoking the self-service method with the target ID. **Proposed rule:** Admin MFA reset is a separately authorized support recovery action. It clears old authenticator/recovery material, disables MFA, revokes sessions, notifies the owner, and records who acted. It must not silently reactivate a held/deleted account.

**Proposed rule:** Require-password-reset is idempotent for an already-required target; it stores when/who required it, invalidates sessions on the first change, and requires a purpose-bound owner recovery path before clearing the flag. Clearing must happen only after a successful password set/reset, in the same Identity transaction. All authentication paths, including future passkeys if implemented, must consult the gate. A passwordless account needs an explicit policy before this operation is allowed.

## Authorization and security

Use bearer Superadmin policy, actor-bound five-minute `X-Step-Up-Proof`, rate limit, and current role recheck before target lookup. Proposed self-target denial prevents an administrator from resetting their own MFA or password obligation using elevated access. Define target eligibility for inactive, deleted, held, locked, and legacy-unclassified states; fail closed and preserve restrictions by default. Missing/invalid proof must not disclose target existence. Audit actor and target IDs with fixed action labels only. Notify the owner through an already verified channel without embedding tokens or sensitive factor details.

## API contract

Both actions take route `userId` and proof header, return `204 No Content` on an approved successful or idempotent transition, and set `Cache-Control: no-store`. Proposed responses: `401`/`403` policy failures, `400` invalid proof or request, `404` missing target after valid proof, `409` ineligible/conflicting target state, `429` rate limit, generic `500` on pre-commit failure. Final target-state/error choices require owner approval. No response carries a code, token, authenticator material, or target profile.

## Entity, persistence, and migration impact

MFA reset can reuse the existing Identity user and token-store fields, subject to confirming complete recovery-code invalidation. Forced reset requires durable, versioned state in `IdentityContext` plus an actor/time provenance decision. An additive migration is expected; review its default for existing users (no reset required), indexes if any, and rollback/data-loss behavior. The migration is generated and reviewed only after explicit approval; deployment application is separate.

## Service decision and evidence

Extend the Identity Account service for admin coordination and the Authentication service for login/recovery gating. Reuse `UserManager`, token-state invalidation, and existing recovery challenge handling. Do not put Identity persistence in an Application repository or duplicate password/token logic in a controller.

## Transaction, audit, and integration impact

Each mutation and token/session invalidation commits atomically in Identity. For forced reset, the requirement must be checked on password login, 2FA completion, refresh and any alternate sign-in before token issuance; clearing is atomic with successful credential replacement. Post-commit notification and explicit audit are best effort under current repository behavior; if guaranteed delivery is required, approve an outbox/retry design. No plaintext secrets in audit/email/logs.

## Test plan

Test policy/proof/self-target gates, all target states, idempotency, rollback, MFA key/recovery-code invalidation, stale bearer and refresh denial, forced-reset denial on every token-issuance path, recovery completion clearing the gate, replay/expiry, passwordless target handling, and post-commit notification/audit failure. Include relational transaction and HTTP tests; verify generic errors and secret redaction.

## Documentation impact

Update the two backlog route checkboxes only after implementation, plus Account and Authentication module docs, security architecture, recovery runbook, and Swagger descriptions. State the support escalation and user recovery path clearly.

## Validation commands

After approval: focused admin credential/recovery tests, `dotnet restore .\AhisApiTemplate.sln`, `dotnet build .\AhisApiTemplate.sln --no-restore`, `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`, and migration SQL review. No database update as routine verification.

## Risks, assumptions, and open questions

- **Blocking:** Who may authorize an MFA reset, and what independent identity proof or support record is required? Superadmin plus step-up authenticates the actor, not the account owner.
- **Blocking:** Which owner recovery flow clears a forced reset, especially for MFA-enabled and passwordless users? A weak email-only bypass could undo the intended control.
- **Blocking:** Should these operations be allowed on held, deleted, or inactive accounts, and should self-targeting be forbidden? The safe proposal is deny and preserve all restrictions.
- **Risk:** The existing self-service MFA reset uses broad Identity operations; integration tests must prove every old recovery code and authenticator secret is unusable after commit.
