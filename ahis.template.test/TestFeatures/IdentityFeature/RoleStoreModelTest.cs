using ahis.template.identity.Contexts;
using ahis.template.identity.Migrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        Assert.Contains("IF EXISTS (SELECT 1 FROM [IdentityRoles])", guard.Sql);
        Assert.Contains(operations, operation => operation is DropTableOperation drop && drop.Name == "IdentityRoles");
        Assert.Contains(operations, operation => operation is RenameTableOperation rename &&
            rename.Name == "AspNetRoles" && rename.NewName == "IdentityRole");
        Assert.DoesNotContain(operations, operation => operation is DropTableOperation drop && drop.Name == "AspNetRoles");
        Assert.DoesNotContain(operations, operation => operation is CreateTableOperation create && create.Name == "IdentityRole");
    }
}
