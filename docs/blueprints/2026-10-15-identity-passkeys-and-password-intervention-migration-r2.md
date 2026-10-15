# Identity passkeys and password intervention migration — revision 2

**Status:** Ready for approval. This revision supersedes `2026-10-15-identity-passkeys-and-password-intervention-migration.md`. No migration has been generated or applied.

## Material revision

`Fido2` 4.3.0 provides the registered public key required for assertion verification but does not expose the exact COSE algorithm identifier through the public registration result used by this implementation. The proposed `PasskeyCredentials.Algorithm` column is therefore removed. `Fido2` verification over the stored public key remains the algorithm source of truth; no placeholder, reflection, or hand-written COSE parser is permitted.

WebAuthn options returned to the browser must contain the raw protocol challenge. The raw challenge is returned only in the transient HTTPS options response; persistence stores a SHA-256 hash plus protected recovery material required to reconstruct the server-side verification options. It is never written to logs, audit data, or API completion responses.

## Context and command

- Context: `ahis.template.identity.Contexts.IdentityContext`
- Migration project: `ahis.template.identity/ahis.template.identity.csproj`
- Startup project: `ahis.template.api/ahis.template.api.csproj`
- Migration name: `AddPasskeyCredentialsAndPasswordResetState`

```powershell
dotnet ef migrations add AddPasskeyCredentialsAndPasswordResetState `
  --project .\ahis.template.identity\ahis.template.identity.csproj `
  --startup-project .\ahis.template.api\ahis.template.api.csproj `
  --context ahis.template.identity.Contexts.IdentityContext `
  --output-dir Migrations
```

## Schema

- `IdentityUsers`: add `MustResetPassword bit NOT NULL DEFAULT 0`.
- `PasskeyCredentials`: UUID key; unique opaque credential ID; required user ID with restrictive FK; public key; signed SQL-compatible sign counter; safe display label; created/last-used timestamps; backup eligibility/state. **No Algorithm column.**
- `PasskeyCeremonies`: UUID key; unique challenge-reference hash; challenge hash; purpose constraint; optional bound user and security version; fixed RP configuration ID; expiry/consumption timestamps; hashed rate-limit keys; protected server-side challenge/options state; restrictive user FK; indexes for ceremony claim and cleanup.

## Rollback and verification

`Down` must refuse if either passkey table has rows, then refuse to remove `MustResetPassword` if any user value is true. It must not alter `IdentityRole`, role assignments, or existing role-store migrations. After generation inspect `Up`, `Down`, designer, snapshot, and generated SQL. Build affected projects and run non-database model tests. SQL Server relational tests for unique credentials, single-use ceremony claim, expiry, concurrency, FKs, and rollback require a separately approved disposable fixture. Do not apply this migration without a named-target inventory and separate approval.
