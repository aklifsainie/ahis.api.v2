# Effective-Superadmin concurrency lock for administrative controls — revision 1

**Status:** Ready for approval. This revision supersedes the administrative disable/enable concurrency portion of `2026-10-14-complete-admin-controls-and-passkeys.md`. It authorizes no implementation, database connection, fixture lifecycle, migration operation, commit, push, or deployment until explicitly approved.

## Outcome

Preserve the invariant that an administrative disable, direct role replacement, account deactivation, or any future in-scope writer cannot leave the system with zero effective Superadmins. Coordinate those writers with one transaction-owned SQL Server application lock before they read or mutate effective-Superadmin state.

## Confirmed source facts

- `IdentityRoleManagementService.ReplaceUserRolesAsync` changes direct role membership under a serializable transaction.
- `AccountService.DeactivateAsync` can remove a user from effective authentication eligibility.
- The planned administrative disable operation would create an `AdministrativeSecurityHold`, also removing effective authentication eligibility.
- Current serializable transactions do not share a common lock protocol. Separate writes can observe a stale effective-Superadmin count and both commit.
- The authoritative role store after `ConsolidateIdentityRoleStore` is `IdentityRole`, not `AspNetRoles`; the focused model test has been corrected to match that migration.

## Shared lock design

1. Add one Identity-owned internal coordination abstraction, for example `IEffectiveSuperadminLock`, implemented through `IdentityContext.Database` on the current transaction connection.
2. Acquire SQL Server `sp_getapplock` with a fixed resource name such as `ahis:effective-superadmin`, `LockMode=Exclusive`, `LockOwner=Transaction`, and a bounded zero/short timeout before any read that determines whether a writer can remove effective Superadmin access.
3. Require a nonnegative `sp_getapplock` result; any timeout, cancellation, unavailable connection, absent transaction, or SQL error fails closed without mutation.
4. The lock is acquired only after actor authorization/proof validation and within the same serializable Identity transaction that performs the final role/restriction/account-state reads and writes.
5. Apply the lock to:
   - direct role replacement when it removes or could remove Superadmin membership;
   - administrative disable before hold placement;
   - account self-deactivation if the target is an effective Superadmin;
   - future writers that can remove role membership or authentication eligibility before their release.
6. Recompute effective Superadmin membership after the lock is held using the authoritative `IdentityRole` store and the exact eligibility predicate: active, not soft-deleted, no active hold or unclassified restriction, no disqualifying lockout, and current Superadmin membership.
7. Refuse a requested mutation if it would reduce the effective count below one. The same transaction writes the mutation and token/session invalidation, then commits. `sp_getapplock` is released automatically at commit/rollback.

## Files

- Add the Identity-owned lock abstraction/implementation under `ahis.template.identity`.
- Modify `IdentityRoleManagementService`, `AccountService`, their interfaces/registration if required, and the planned disable/enable service path.
- Add focused unit tests for lock acquisition failure/result mapping and SQL Server relational concurrency tests for competing role removal, disable, and deactivation.
- Update identity administration, authentication/authorization, persistence/testing documentation, and the backlog only after verified delivery.

## Tests and validation

- Test lock acquisition occurs after authorization/proof validation and before effective-count reads.
- Test timeout/failure causes generic fail-closed result and no mutation.
- Relational SQL Server tests must run competing transactions for: two Superadmin disables, disable versus role replacement, deactivation versus role replacement, and valid mutations that preserve at least one effective Superadmin.
- Verify one mutation is refused/rolled back where two would otherwise remove the final effective administrator.
- Run focused tests, Identity/API builds, then solution build/tests. Relational tests require a separately approved disposable SQL Server fixture; never use an operational database.

## Risks and gates

`sp_getapplock` is SQL Server-specific and intentionally couples this Identity concurrency invariant to the repository's existing SQL Server provider. Changing the provider, lock resource, ownership semantics, participating writers, or timeout policy requires a revised review. This revision has no schema/migration change. Fixture creation/use/deletion, database access, commit, push, migration generation/application, and deployment remain separate gates.
