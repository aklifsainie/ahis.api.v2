# Blueprint: Identity role-store alignment and initial role seeding

**Status:** Draft — explicit approval required before implementation.

## Outcome

Make ASP.NET Core Identity's configured role store and `IdentityContext` role mapping agree, then generate one safe migration that seeds the initial `Superadmin` and `User` roles into the authoritative table. This is prerequisite work for the administrative security-state endpoint's `Superadmin` authorization policy.

## Confirmed discovery

- `Program.ConfigureAuthentication` configures `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<IdentityContext>()`. Runtime `RoleManager`/`UserManager` role operations therefore use `IdentityRole`.
- `IdentityContext` explicitly maps `IdentityRole<string>` to `IdentityRoles`.
- The generated initial migration creates both `AspNetRoles` and `IdentityRoles`, and its `IdentityRoleClaims` foreign key targets `AspNetRoles`.
- A discarded, un-applied trial migration generated inserts into `IdentityRoles`; that would not align with the configured runtime `IdentityRole` store. It was removed, and no database migration was applied.

## Required design decision

Before any code or migration is changed, inspect the target database's `__EFMigrationsHistory`, `AspNetRoles`, `IdentityRoles`, `IdentityUserRoles`, and `IdentityRoleClaims` schema/data. Confirm which table contains—or must contain—live role assignments. Do not infer this from source alone.

The preferred direction is to make `IdentityRole` the single mapped runtime entity and preserve the table that is already referenced by the applied user-role and role-claim relationships. If a table rename, data transfer, foreign-key repair, or role-assignment migration is required, it must be separately specified and approved after database inspection.

## Files likely affected

- `ahis.template.api/Program.cs` — only if the selected `AddRoles<T>` type or policy wiring must change.
- `ahis.template.identity/Contexts/IdentityContext.cs` — align the role entity mapping with the selected runtime store; seed roles only after alignment.
- `ahis.template.identity/Security/IdentityRoleNames.cs` — retain canonical `Superadmin` and `User` names.
- `ahis.template.identity/Migrations/<timestamp>_...cs`, designer, and snapshot — generated only after the correct context model is approved.
- Focused integration tests for `RoleManager` creation/lookup, user role assignment, JWT role claim emission, and `IdentityAdminSecurityReadPolicy` authorization.
- `docs/backlog/identity-security-endpoints.md` and the security-state blueprint after completion.

## Migration constraints

- Context: `IdentityContext`.
- Migration project: `ahis.template.identity/ahis.template.identity.csproj`.
- Startup project: `ahis.template.api/ahis.template.api.csproj`.
- Do not hand-author role inserts before the correct role table is confirmed.
- Do not apply any migration under this blueprint. Generation, source review, and application remain separate approvals.
- The migration must be reversible where feasible and explicitly protect existing users, role assignments, claims, and audit data.

## Validation

1. Inspect the target database schema and migration history under separate database-read authorization.
2. Generate the approved migration only after the data/table decision is recorded.
3. Inspect every `Up`, `Down`, and model-snapshot change.
4. Build `AhisApiTemplate.sln` with `--no-restore`.
5. Run focused Identity integration tests, then `dotnet test AhisApiTemplate.sln --no-build --no-restore`.
6. Do not run `dotnet ef database update` without separate explicit authorization for the named environment.

## Risks

- Seeding the table not used by `RoleManager` creates roles that cannot authorize users.
- Repairing a duplicate role-table model can affect live assignments and role claims.
- Existing JWTs do not gain a newly assigned role until a fresh token is issued; changing role membership may also require the separate token-version/revocation decision tracked in the backlog.
