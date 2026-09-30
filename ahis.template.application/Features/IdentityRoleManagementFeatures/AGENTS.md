# Identity Administration Feature Instructions

Identity Administration owns human-role catalog reads and administration, user-role assignment, server-owned human permission catalog reads, and first-Superadmin bootstrap. It spans the Application feature, admin controllers, Identity role-management and bootstrap services, `IdentityContext`, and Identity migrations.

Controllers dispatch through the custom mediator; `IdentityRoleManagementHandler` delegates privileged state changes to `IIdentityRoleManagementService`. Keep `RoleManager`, `UserManager`, protected-role checks, permission validation, concurrency, transactions, and target session invalidation in that Identity-service boundary. Do not use the Country repository/unit-of-work pattern for this module.

Before changing it, inspect the affected admin controller action, `IdentityRoleManagementRequests.cs`, `IIdentityRoleManagementService`, `IdentityRoleManagementService`, `IdentityRoleBootstrapService`, `HumanPermissionCatalog`, `Program.cs` policies, `IdentityContext`, and [the module guide](../../../docs/modules/identity-administration.md).

Observed security invariants: reads require bearer Superadmin authorization; role, permission, and user-role writes also require an actor-bound step-up proof and authenticated-security rate limit. `Superadmin` and `User` are protected roles; permission writes use ETags and invalidate assigned users' token/session state after a material change. Treat changes as authorization-, security-, and migration-sensitive blueprinted work. Focused tests may require SQL Server LocalDB.
