# Identity passkeys and password intervention migration

**Status:** Draft for migration approval; source implementation incomplete. No migration has been generated or applied.

## Context and projects

- Context: `ahis.template.identity.Contexts.IdentityContext`.
- Migration project: `ahis.template.identity/ahis.template.identity.csproj`.
- Startup project: `ahis.template.api/ahis.template.api.csproj`.
- Provider: SQL Server; Identity migrations and snapshot live in `ahis.template.identity/Migrations`.
- Latest migration observed in source: `20260929034815_AddIdentityRoleBootstrapState`; the consolidated role store maps `IdentityRole` to the existing `IdentityRole` table. Preserve that role mapping and all existing role assignments.
- Proposed migration name: `AddPasskeyCredentialsAndPasswordResetState`.

## Proposed schema

### `IdentityUsers`

- Add `MustResetPassword bit NOT NULL` with database default `0` for existing users and model default `false`.
- The column is configured explicitly with EF Core `HasDefaultValue(false)` so EF scaffolded deployments and existing rows receive the same safe default; this implementation does not generate migration artifacts.
- No rename or drop. Existing users retain ordinary sign-in behavior.

### `PasskeyCredentials`

- `Id uniqueidentifier NOT NULL` primary key.
- `CredentialId nvarchar(1024) NOT NULL` (opaque base64url credential identifier); unique index `IX_PasskeyCredentials_CredentialId`.
- `UserId nvarchar(450) NOT NULL`, FK to `IdentityUsers.Id`, `ON DELETE NO ACTION`/restrict.
- `PublicKey varbinary(max) NOT NULL`; public verification key only, never private material.
- `SignCount bigint NOT NULL` (unsigned protocol count represented in a SQL-compatible signed range; implementation must reject values above `Int64.MaxValue`).
- `DisplayName nvarchar(80) NOT NULL` (safe user label).
- `CreatedAtUtc datetime2 NOT NULL`, `LastUsedAtUtc datetime2 NULL`.
- `IsBackupEligible bit NOT NULL DEFAULT 0`, `IsBackedUp bit NOT NULL DEFAULT 0`.
- Owner lookup index `IX_PasskeyCredentials_UserId_CreatedAtUtc` on `(UserId, CreatedAtUtc)`.
- Active limit of 10 is enforced transactionally in service code. No filtered index can enforce a row count limit.

### `PasskeyCeremonies`

- `Id uniqueidentifier NOT NULL` primary key.
- `ChallengeReferenceHash binary(32) NOT NULL`, unique index `IX_PasskeyCeremonies_ChallengeReferenceHash`.
- `ChallengeHash binary(32) NOT NULL`; only a hash of the random challenge is persisted.
- `Purpose int NOT NULL` (`1=Registration`, `2=Authentication`); check constraint to those values.
- `UserId nvarchar(450) NULL`, FK to `IdentityUsers.Id`, `ON DELETE NO ACTION`/restrict. Anonymous authentication ceremonies have no known user at issue time.
- `SecurityVersion nvarchar(64) NULL`; required by service logic for known-user registration, null for anonymous authentication options.
- `RpConfigurationId nvarchar(100) NOT NULL`; fixed development configuration identity, not a caller-controlled RP value.
- `ExpiresAtUtc datetime2 NOT NULL` (five-minute expiry), `ConsumedAtUtc datetime2 NULL`.
- `CreatedAtUtc datetime2 NOT NULL`, used to enforce one-minute ceremony creation limits per hashed IP and, where known, per user. The persisted `RateLimitKeyHash` stores only a SHA-256 hex digest of the IP key.
- `RateLimitKeyHash nvarchar(64) NOT NULL`; hashed key material only, never raw IP/account identifiers.
- `UserRateLimitKeyHash nvarchar(64) NULL`; a SHA-256 hex digest of a known account identifier, used for five-per-minute known-account throttling without storing raw account identifiers.
- `OptionsJson nvarchar(max) NOT NULL`; the Fido2 4.3.0 serialized options are retained without the `challenge` member. The original challenge is restored for library verification from `ProtectedChallenge`.
- `ProtectedChallenge varbinary(max) NOT NULL`; the random challenge protected using ASP.NET Core Data Protection with the fixed purpose `AHIS.Passkeys.Challenge.v1`. Only `ChallengeHash` is a plaintext challenge-derived value. Deployment must preserve the Data Protection key ring across instances and restarts before passkeys are enabled; production remains disabled in this increment.
- Index `IX_PasskeyCeremonies_Purpose_UserId_ExpiresAtUtc_ConsumedAtUtc` for active ceremony/cleanup lookups. A unique filtered index on active rows is intentionally omitted because simultaneous distinct ceremonies are allowed; single-use completion must atomically claim one row by `ConsumedAtUtc IS NULL AND ExpiresAtUtc > now`.

## Existing data, ordering, and risks

- The approved revision removes `Algorithm`: Fido2 verification over the registered public key remains the algorithm source of truth. No placeholder, reflection, or hand-written COSE parser is used.
- Required Fido2-vector and SQL Server ceremony/replay/concurrency tests have not been implemented or run. LocalDB-dependent tests are prohibited in the current implementation request.

- Adding a non-null `MustResetPassword` column is safe for existing users only with default `0`.
- New passkey tables start empty; there is no backfill.
- Restrictive user FKs prevent deleting an account with passkey/ceremony records; cleanup must explicitly delete expired ceremonies and credential deletion must be owner-checked.
- `Down` may drop `PasskeyCeremonies` and `PasskeyCredentials` only if both tables contain no rows; otherwise it must throw before destructive operations. Then remove `MustResetPassword` only if every value is `0`; otherwise throw. This retains credentials and administrative interventions during rollback rather than silently losing them.
- `Down` must not alter `IdentityRole`, `IdentityUserRoles`, or any consolidated role-store state.
- Do not apply this migration until source review, generated SQL review, and inventory of the actual target's migration history are separately approved.

## Exact generation command (not run)

```powershell
dotnet ef migrations add AddPasskeyCredentialsAndPasswordResetState `
  --project .\ahis.template.identity\ahis.template.identity.csproj `
  --startup-project .\ahis.template.api\ahis.template.api.csproj `
  --context ahis.template.identity.Contexts.IdentityContext `
  --output-dir Migrations
```

## Verification after separate generation approval

1. Inspect migration operations, designer, and `IdentityContextModelSnapshot` for only the columns, tables, constraints, and indexes above.
2. Inspect generated SQL and verify the guarded `Down` checks precede all drops.
3. Build the Identity and API projects and run non-database Identity model tests.
4. Add SQL Server tests for credential uniqueness, restrictive FKs, atomic challenge consumption, expiration, protected-option/challenge binding, rate limiting, and concurrency. Run them only with a separately approved disposable database fixture.
5. Never run `dotnet ef database update` as routine verification.
