using ahis.template.identity.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using ahis.template.identity.Migrations;
using FluentAssertions;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public class IdentityContextRoleStoreMappingTest
{
    [Fact]
    public void Model_has_safe_required_default_for_password_reset_flag()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=IdentityContextRoleStoreMappingTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var context = new IdentityContext(options);
        var property = context.Model.FindEntityType(typeof(ahis.template.identity.Models.Entities.ApplicationUser))!
            .FindProperty(nameof(ahis.template.identity.Models.Entities.ApplicationUser.MustResetPassword))!;

        property.IsNullable.Should().BeFalse();
        property.GetDefaultValue().Should().Be(false);
    }

    [Fact]
    public void Passkey_model_has_unique_credential_and_hashed_single_use_ceremony_indexes()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True").Options;
        using var context = new IdentityContext(options);
        var credential = context.Model.FindEntityType(typeof(ahis.template.identity.Models.Entities.PasskeyCredential))!;
        credential.GetTableName().Should().Be("PasskeyCredentials");
        credential.FindProperty("Algorithm").Should().BeNull();
        credential.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Single().Name == "CredentialId");
        var ceremony = context.Model.FindEntityType(typeof(ahis.template.identity.Models.Entities.PasskeyCeremony))!;
        ceremony.GetTableName().Should().Be("PasskeyCeremonies");
        ceremony.FindProperty(nameof(ahis.template.identity.Models.Entities.PasskeyCeremony.ChallengeReferenceHash))!.GetColumnType().Should().Be("binary(32)");
        ceremony.FindProperty(nameof(ahis.template.identity.Models.Entities.PasskeyCeremony.ChallengeHash))!.GetColumnType().Should().Be("binary(32)");
        ceremony.GetForeignKeys().Single().DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Browser_passkey_options_keep_nonempty_challenge_and_no_empty_placeholder()
    {
        const string serialized = "{\"challenge\":\"AQIDBA\",\"rp\":{\"name\":\"AHIS\"},\"pubKeyCredParams\":[{\"type\":\"public-key\",\"alg\":-7}]}";
        var options = ahis.template.identity.Services.PasskeyService.ToBrowserOptions(serialized);

        options.GetProperty("challenge").GetString().Should().Be("AQIDBA");
        options.GetProperty("challenge").GetString().Should().NotBeEmpty();
        options.TryGetProperty("algorithm", out _).Should().BeFalse();

        var persisted = ahis.template.identity.Services.PasskeyService.StripChallenge(serialized);
        persisted.GetProperty("challenge").GetString().Should().BeEmpty();
    }

    [Fact]
    public void Model_maps_only_the_concrete_identity_role_to_the_consolidated_role_table()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=IdentityContextRoleStoreMappingTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var context = new IdentityContext(options);
        var roleEntityTypes = context.Model.GetEntityTypes()
            .Where(entityType => entityType.ClrType == typeof(IdentityRole))
            .ToList();

        roleEntityTypes.Should().ContainSingle();
        roleEntityTypes[0].GetTableName().Should().Be("IdentityRole");
        context.Model.FindEntityType(typeof(IdentityRole<string>)).Should().BeNull();
        context.Model.FindEntityType(typeof(IdentityUserRole<string>))!.GetForeignKeys()
            .Single(foreignKey => foreignKey.Properties.Single().Name == "RoleId")
            .PrincipalEntityType.ClrType.Should().Be(typeof(IdentityRole));
        context.Model.FindEntityType(typeof(IdentityRoleClaim<string>))!.GetForeignKeys()
            .Single(foreignKey => foreignKey.Properties.Single().Name == "RoleId")
            .PrincipalEntityType.ClrType.Should().Be(typeof(IdentityRole));
    }

    [Fact]
    public void Migration_contains_guarded_role_store_alignment_and_safe_rollback_sql()
    {
        var migration = new TestMigration();
        var upBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var downBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        migration.BuildUp(upBuilder);
        migration.BuildDown(downBuilder);

        var upSql = upBuilder.Operations.OfType<SqlOperation>().Should().ContainSingle().Subject.Sql;
        var downSql = downBuilder.Operations.OfType<SqlOperation>().Should().ContainSingle().Subject.Sql;

        upSql.Should().Contain("IdentityRoles contains data");
        upSql.Should().Contain("IdentityRoles has foreign-key dependents");
        upSql.Should().Contain("IdentityRoles has trigger dependencies");
        upSql.Should().Contain("A protected role seed ID is already in use");
        upSql.Should().Contain("DROP TABLE [dbo].[IdentityRoles]");
        upSql.Should().Contain("Superadmin");
        upSql.Should().Contain("User");
        upSql.Should().Contain("Role assignments or claims changed unexpectedly");
        downSql.Should().Contain("IdentityRoles already exists; rollback is unsafe");
        downSql.Should().Contain("Migration-owned roles changed or referenced; rollback is unsafe");
        downSql.Should().Contain("CREATE TABLE [dbo].[IdentityRoles]");
    }

    private sealed class TestMigration : AlignIdentityRoleStoreAndSeedSystemRoles
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => base.Up(migrationBuilder);

        public void BuildDown(MigrationBuilder migrationBuilder) => base.Down(migrationBuilder);
    }
}
