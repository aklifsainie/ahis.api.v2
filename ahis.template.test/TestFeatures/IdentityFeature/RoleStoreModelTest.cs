using ahis.template.identity.Contexts;
using ahis.template.identity.Migrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public class RoleStoreModelTest
{
    [Fact]
    public void ConfiguredRoleAndRelationshipsUseOneRenamedTable()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RoleStoreModelTest;Trusted_Connection=True")
            .Options;
        using var context = new IdentityContext(options);

        var role = context.Model.FindEntityType(typeof(IdentityRole));
        Assert.NotNull(role);
        Assert.Equal("IdentityRole", role.GetTableName());
        Assert.Null(context.Model.FindEntityType(typeof(IdentityRole<string>)));
        Assert.Contains(role.GetIndexes(), index =>
            index.GetDatabaseName() == "RoleNameIndex" && index.IsUnique);

        foreach (var dependentType in new[] { typeof(IdentityUserRole<string>), typeof(IdentityRoleClaim<string>) })
        {
            var dependent = context.Model.FindEntityType(dependentType);
            Assert.NotNull(dependent);
            Assert.Contains(dependent.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType == role);
        }
    }

    [Fact]
    public void MigrationGuardsDuplicateTableAndRenamesOperationalTable()
    {
        var operations = new ConsolidateIdentityRoleStore().UpOperations;

        var guard = Assert.IsType<SqlOperation>(operations[0]);
        Assert.Contains("OBJECT_ID(N'[dbo].[IdentityRoles]', N'U') IS NULL", guard.Sql);
        Assert.Contains("CREATE TABLE [dbo].[IdentityRoles]", guard.Sql);
        Assert.Contains("IdentityRoles contains rows; reconcile them before consolidating the role store.", guard.Sql);
        Assert.Contains(operations, operation => operation is DropTableOperation drop && drop.Name == "IdentityRoles");
        Assert.Contains(operations, operation => operation is RenameTableOperation rename &&
            rename.Name == "AspNetRoles" && rename.NewName == "IdentityRole");
        Assert.DoesNotContain(operations, operation => operation is DropTableOperation drop && drop.Name == "AspNetRoles");
        Assert.DoesNotContain(operations, operation => operation is CreateTableOperation create && create.Name == "IdentityRole");
    }

    [Fact]
    public void BootstrapStateUsesASingletonMarkerWithRestrictedTarget()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RoleBootstrapModelTest;Trusted_Connection=True")
            .Options;
        using var context = new IdentityContext(options);

        var marker = context.Model.FindEntityType(
            typeof(ahis.template.identity.Models.Entities.IdentityRoleBootstrapState));
        Assert.NotNull(marker);
        Assert.Equal("IdentityRoleBootstrapState", marker.GetTableName());

        var foreignKey = marker.GetForeignKeys().Single();
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal("TargetUserId", foreignKey.Properties.Single().Name);
        Assert.Equal("IdentityUsers", foreignKey.PrincipalEntityType.GetTableName());

        var operations = new AddIdentityRoleBootstrapState().UpOperations;
        var create = Assert.Single(operations.OfType<CreateTableOperation>(), operation =>
            operation.Name == "IdentityRoleBootstrapState");
        Assert.Contains(create.CheckConstraints, constraint =>
            constraint.Name == "CK_IdentityRoleBootstrapState_Singleton" && constraint.Sql == "[Id] = 1");
        Assert.Contains(create.ForeignKeys, constraint =>
            constraint.PrincipalTable == "IdentityUsers" &&
            constraint.OnDelete == ReferentialAction.Restrict);
    }
}
