using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Security;
using ahis.template.identity.Services;
using ahis.template.domain.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public sealed class IdentityRoleManagementServiceTest
{
    [Fact]
    public async Task ReplacesRolesReturnsSortedDeltaAndInvalidatesTarget()
    {
        await using var fixture = await RoleManagementFixture.CreateAsync();
        var target = await fixture.AddUserAsync("target");
        var existing = await fixture.AddRoleAsync("Existing");
        var addedB = await fixture.AddRoleAsync("AddedB");
        var addedA = await fixture.AddRoleAsync("AddedA");
        await fixture.Users.AddToRoleAsync(target, existing.Name!);
        fixture.Tokens.Setup(x => x.InvalidateAsync(target, It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);

        var result = await fixture.Service.ReplaceUserRolesAsync("actor", "proof", target.Id, [addedB.Id, addedA.Id], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Changed.Should().BeTrue();
        result.Value.AddedRoleIds.Should().Equal(new[] { addedA.Id, addedB.Id }.OrderBy(id => id, StringComparer.Ordinal));
        result.Value.RemovedRoleIds.Should().Equal(existing.Id);
        (await fixture.Users.GetRolesAsync(target)).Should().BeEquivalentTo([addedA.Name!, addedB.Name!]);
        fixture.Tokens.Verify(x => x.InvalidateAsync(target, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoOpReplacementDoesNotInvalidateTarget()
    {
        await using var fixture = await RoleManagementFixture.CreateAsync();
        var target = await fixture.AddUserAsync("target");
        var role = await fixture.AddRoleAsync("Member");
        await fixture.Users.AddToRoleAsync(target, role.Name!);

        var result = await fixture.Service.ReplaceUserRolesAsync("actor", "proof", target.Id, [role.Id], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Changed.Should().BeFalse();
        result.Value.AddedRoleIds.Should().BeEmpty();
        result.Value.RemovedRoleIds.Should().BeEmpty();
        fixture.Tokens.Verify(x => x.InvalidateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RollsBackMembershipChangesWhenTargetInvalidationFails()
    {
        await using var fixture = await RoleManagementFixture.CreateAsync();
        var target = await fixture.AddUserAsync("target");
        var existing = await fixture.AddRoleAsync("Existing");
        var replacement = await fixture.AddRoleAsync("Replacement");
        await fixture.Users.AddToRoleAsync(target, existing.Name!);
        fixture.Tokens.Setup(x => x.InvalidateAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failure" }));

        var result = await fixture.Service.ReplaceUserRolesAsync("actor", "proof", target.Id, [replacement.Id], CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        (await fixture.Users.GetRolesAsync(target)).Should().Equal(existing.Name!);
    }

    [Fact]
    public async Task CreateRoleReturnsValidationFailureInsteadOfThrowingWhenActorCannotBeAuthorized()
    {
        await using var fixture = await RoleManagementFixture.CreateAsync();

        var result = await fixture.Service.CreateRoleAsync("missing-actor", "proof", "RoleTest", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Metadata.Should().ContainKey("Validation");
    }

    [Fact]
    public async Task ReplacesCustomRolePermissionsAndInvalidatesEveryAssignedUser()
    {
        await using var fixture = await RoleManagementFixture.CreateAsync();
        var role = await fixture.AddRoleAsync("CountryReader");
        var first = await fixture.AddUserAsync("first");
        var second = await fixture.AddUserAsync("second");
        await fixture.Users.AddToRoleAsync(first, role.Name!);
        await fixture.Users.AddToRoleAsync(second, role.Name!);
        fixture.Tokens.Setup(x => x.InvalidateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);
        var etag = (await fixture.Service.GetRoleAsync(role.Id, CancellationToken.None)).Value.ETag;

        var result = await fixture.Service.ReplaceRolePermissionsAsync("actor", "proof", role.Id, etag, [HumanPermissionCatalog.CountryRead], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Changed.Should().BeTrue();
        result.Value.AddedPermissions.Should().Equal(HumanPermissionCatalog.CountryRead);
        (await fixture.Service.GetRolePermissionsAsync(role.Id, CancellationToken.None)).Value.Should().Equal(HumanPermissionCatalog.CountryRead);
        fixture.Tokens.Verify(x => x.InvalidateAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Tokens.Verify(x => x.InvalidateAsync(second, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoOpPermissionReplacementDoesNotInvalidateUsers()
    {
        await using var fixture = await RoleManagementFixture.CreateAsync();
        var role = await fixture.AddRoleAsync("CountryReader");
        await fixture.Roles.AddClaimAsync(role, new System.Security.Claims.Claim(HumanPermissionCatalog.ClaimType, HumanPermissionCatalog.CountryRead));
        var etag = (await fixture.Service.GetRoleAsync(role.Id, CancellationToken.None)).Value.ETag;

        var result = await fixture.Service.ReplaceRolePermissionsAsync("actor", "proof", role.Id, etag, [HumanPermissionCatalog.CountryRead], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Changed.Should().BeFalse();
        fixture.Tokens.Verify(x => x.InvalidateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectsProtectedRoleAndUnknownPermission()
    {
        await using var fixture = await RoleManagementFixture.CreateAsync();
        var protectedRole = await fixture.Context.Roles.SingleAsync(role => role.Name == IdentityRoleNames.User);
        var protectedTag = (await fixture.Service.GetRoleAsync(protectedRole.Id, CancellationToken.None)).Value.ETag;
        var custom = await fixture.AddRoleAsync("Custom");
        var customTag = (await fixture.Service.GetRoleAsync(custom.Id, CancellationToken.None)).Value.ETag;

        var protectedResult = await fixture.Service.ReplaceRolePermissionsAsync("actor", "proof", protectedRole.Id, protectedTag, [], CancellationToken.None);
        var invalidResult = await fixture.Service.ReplaceRolePermissionsAsync("actor", "proof", custom.Id, customTag, ["unknown.permission"], CancellationToken.None);

        protectedResult.IsFailed.Should().BeTrue();
        protectedResult.Errors.Single().Metadata.Should().ContainKey("Conflict");
        invalidResult.IsFailed.Should().BeTrue();
        invalidResult.Errors.Single().Metadata.Should().ContainKey("Validation");
    }

    private sealed class RoleManagementFixture : IAsyncDisposable
    {
        private readonly string _connectionString;
        public IdentityContext Context { get; }
        public UserManager<ApplicationUser> Users { get; }
        public RoleManager<IdentityRole> Roles { get; }
        public Mock<IIdentityTokenStateService> Tokens { get; }
        public IdentityRoleManagementService Service { get; }

        private RoleManagementFixture(string connectionString, IdentityContext context, UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles, Mock<IIdentityTokenStateService> tokens, IdentityRoleManagementService service)
        {
            _connectionString = connectionString;
            Context = context;
            Users = users;
            Roles = roles;
            Tokens = tokens;
            Service = service;
        }

        public static async Task<RoleManagementFixture> CreateAsync()
        {
            var databaseName = $"AhisRoleManagementTest_{Guid.NewGuid():N}";
            var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
            var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>().UseSqlServer(connectionString).Options);
            await context.Database.EnsureCreatedAsync();

            var normalizer = new UpperInvariantLookupNormalizer();
            var userStore = new UserStore<ApplicationUser, IdentityRole, IdentityContext, string>(context);
            var roleStore = new RoleStore<IdentityRole, IdentityContext, string>(context);
            var users = new UserManager<ApplicationUser>(userStore, Options.Create(new IdentityOptions()), new PasswordHasher<ApplicationUser>(),
                [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()], normalizer, new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(), NullLogger<UserManager<ApplicationUser>>.Instance);
            var roles = new RoleManager<IdentityRole>(roleStore, [new RoleValidator<IdentityRole>()], normalizer, new IdentityErrorDescriber(), NullLogger<RoleManager<IdentityRole>>.Instance);
            var proof = new Mock<IAccountSecurityProofService>();
            proof.Setup(x => x.IsValidAsync("actor", "proof", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var tokens = new Mock<IIdentityTokenStateService>();
            tokens.Setup(x => x.IsEligible(It.IsAny<ApplicationUser>())).Returns(true);
            var restrictions = new Mock<IIdentityRestrictionService>();
            restrictions.Setup(x => x.IsAuthenticationAllowedAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var service = new IdentityRoleManagementService(context, roles, users, normalizer, proof.Object, tokens.Object, restrictions.Object);
            var superadmin = new IdentityRole(IdentityRoleNames.Superadmin);
            (await roles.CreateAsync(superadmin)).Succeeded.Should().BeTrue();
            (await roles.CreateAsync(new IdentityRole(IdentityRoleNames.User))).Succeeded.Should().BeTrue();
            var actor = new ApplicationUser { Id = "actor", UserName = "actor", Email = "actor@example.test", IsActive = true, EmailConfirmed = true };
            (await users.CreateAsync(actor)).Succeeded.Should().BeTrue();
            (await users.AddToRoleAsync(actor, IdentityRoleNames.Superadmin)).Succeeded.Should().BeTrue();
            return new RoleManagementFixture(connectionString, context, users, roles, tokens, service);
        }

        public async Task<ApplicationUser> AddUserAsync(string userName)
        {
            var user = new ApplicationUser { UserName = userName, Email = $"{userName}@example.test", IsActive = true, EmailConfirmed = true };
            (await Users.CreateAsync(user)).Succeeded.Should().BeTrue();
            return user;
        }

        public async Task<IdentityRole> AddRoleAsync(string name)
        {
            var role = new IdentityRole(name);
            (await Roles.CreateAsync(role)).Succeeded.Should().BeTrue();
            return role;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.Database.EnsureDeletedAsync();
            await Context.DisposeAsync();
            Users.Dispose();
            Roles.Dispose();
        }
    }
}
