using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class AlignIdentityRoleStoreAndSeedSystemRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                IF OBJECT_ID(N'[dbo].[IdentityRoles]', N'U') IS NULL
                    THROW 51000, 'IdentityRoles is missing.', 1;
                IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NULL
                    THROW 51000, 'AspNetRoles is missing.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[IdentityRoles])
                    THROW 51000, 'IdentityRoles contains data and cannot be dropped.', 1;
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'[dbo].[IdentityRoles]') OR parent_object_id = OBJECT_ID(N'[dbo].[IdentityRoles]'))
                    THROW 51000, 'IdentityRoles has foreign-key dependents and cannot be dropped.', 1;
                IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[IdentityRoles]')) <> 4
                    THROW 51000, 'IdentityRoles has an unexpected schema.', 1;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[IdentityRoles]') AND name NOT IN (N'Id', N'Name', N'NormalizedName', N'ConcurrencyStamp'))
                    THROW 51000, 'IdentityRoles has an unexpected schema.', 1;
                IF EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = OBJECT_ID(N'[dbo].[IdentityRoles]'))
                    THROW 51000, 'IdentityRoles has trigger dependencies and cannot be dropped.', 1;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'RoleNameIndex' AND is_unique = 1)
                    THROW 51000, 'AspNetRoles does not have the expected unique RoleNameIndex.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE ([NormalizedName] = N'SUPERADMIN' AND [Name] <> N'Superadmin') OR ([NormalizedName] = N'USER' AND [Name] <> N'User') OR ([Name] = N'Superadmin' AND [NormalizedName] <> N'SUPERADMIN') OR ([Name] = N'User' AND [NormalizedName] <> N'USER'))
                    THROW 51000, 'A protected role has inconsistent casing.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Id] IN (N'7D4D078A-AF46-4B3E-92E4-925AD2E0A1D0', N'8E304A5E-8190-44DF-9FC4-29B601F2B640'))
                    THROW 51000, 'A protected role seed ID is already in use.', 1;
                DECLARE @userRoleCountBefore int = (SELECT COUNT(*) FROM [dbo].[IdentityUserRoles]);
                DECLARE @roleClaimCountBefore int = (SELECT COUNT(*) FROM [dbo].[IdentityRoleClaims]);
                DROP TABLE [dbo].[IdentityRoles];
                IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = N'SUPERADMIN')
                    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'7D4D078A-AF46-4B3E-92E4-925AD2E0A1D0', N'Superadmin', N'SUPERADMIN', N'B1A5D2F6-32E4-4F44-87C9-DA987951B8A1');
                IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = N'USER')
                    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'8E304A5E-8190-44DF-9FC4-29B601F2B640', N'User', N'USER', N'7DE70670-E9D4-46C6-94C4-0B1E51FB6F3E');
                IF (SELECT COUNT(*) FROM [dbo].[AspNetRoles] WHERE [NormalizedName] IN (N'SUPERADMIN', N'USER')) <> 2
                    THROW 51000, 'Protected role seed verification failed.', 1;
                IF (SELECT COUNT(*) FROM [dbo].[IdentityUserRoles]) <> @userRoleCountBefore OR (SELECT COUNT(*) FROM [dbo].[IdentityRoleClaims]) <> @roleClaimCountBefore
                    THROW 51000, 'Role assignments or claims changed unexpectedly.', 1;
                COMMIT TRANSACTION;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                IF OBJECT_ID(N'[dbo].[IdentityRoles]', N'U') IS NOT NULL
                    THROW 51000, 'IdentityRoles already exists; rollback is unsafe.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE ([Id] = N'7D4D078A-AF46-4B3E-92E4-925AD2E0A1D0' AND ([Name] <> N'Superadmin' OR [NormalizedName] <> N'SUPERADMIN' OR [ConcurrencyStamp] <> N'B1A5D2F6-32E4-4F44-87C9-DA987951B8A1')) OR ([Id] = N'8E304A5E-8190-44DF-9FC4-29B601F2B640' AND ([Name] <> N'User' OR [NormalizedName] <> N'USER' OR [ConcurrencyStamp] <> N'7DE70670-E9D4-46C6-94C4-0B1E51FB6F3E')) OR EXISTS (SELECT 1 FROM [dbo].[IdentityUserRoles] ur WHERE ur.RoleId = [AspNetRoles].[Id]) OR EXISTS (SELECT 1 FROM [dbo].[IdentityRoleClaims] rc WHERE rc.RoleId = [AspNetRoles].[Id])))
                    THROW 51000, 'Migration-owned roles changed or referenced; rollback is unsafe.', 1;
                DELETE FROM [dbo].[AspNetRoles] WHERE ([Id] = N'7D4D078A-AF46-4B3E-92E4-925AD2E0A1D0' AND [Name] = N'Superadmin' AND [NormalizedName] = N'SUPERADMIN' AND [ConcurrencyStamp] = N'B1A5D2F6-32E4-4F44-87C9-DA987951B8A1') OR ([Id] = N'8E304A5E-8190-44DF-9FC4-29B601F2B640' AND [Name] = N'User' AND [NormalizedName] = N'USER' AND [ConcurrencyStamp] = N'7DE70670-E9D4-46C6-94C4-0B1E51FB6F3E');
                CREATE TABLE [dbo].[IdentityRoles] ([Id] nvarchar(450) NOT NULL, [Name] nvarchar(max) NULL, [NormalizedName] nvarchar(max) NULL, [ConcurrencyStamp] nvarchar(max) NULL, CONSTRAINT [PK_IdentityRoles] PRIMARY KEY ([Id]));
                COMMIT TRANSACTION;
                """);
        }
    }
}
