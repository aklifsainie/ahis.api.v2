using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Security;
using ahis.template.identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ahis.template.test.TestFeatures.ApiKeySecurity;

public class IdentityApiClientAdministrationServiceTest
{
    [Fact]
    public async Task ValidatesCurrentSuperadminAndActorBoundProof()
    {
        var user = new ApplicationUser { Id = "admin", IsActive = true, IsDeleted = false };
        var users = CreateUserManager();
        var proof = new Mock<IAccountSecurityProofService>();
        var tokens = new Mock<IIdentityTokenStateService>();
        users.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        users.Setup(x => x.IsInRoleAsync(user, IdentityRoleNames.Superadmin)).ReturnsAsync(true);
        proof.Setup(x => x.IsValidAsync(user.Id, "proof", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        tokens.Setup(x => x.IsEligible(user)).Returns(true);
        var service = new IdentityApiClientAdministrationService(users.Object, proof.Object, tokens.Object);

        var result = await service.ValidateAsync(user.Id, "proof");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RejectsNonSuperadminBeforeProofValidation()
    {
        var user = new ApplicationUser { Id = "user", IsActive = true, IsDeleted = false };
        var users = CreateUserManager();
        var proof = new Mock<IAccountSecurityProofService>();
        var tokens = new Mock<IIdentityTokenStateService>();
        users.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        users.Setup(x => x.IsInRoleAsync(user, IdentityRoleNames.Superadmin)).ReturnsAsync(false);
        tokens.Setup(x => x.IsEligible(user)).Returns(true);
        var service = new IdentityApiClientAdministrationService(users.Object, proof.Object, tokens.Object);

        var result = await service.ValidateAsync(user.Id, "proof");

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, error => error.Metadata.ContainsKey("Forbidden"));
        proof.Verify(x => x.IsValidAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectsInvalidProofForCurrentSuperadmin()
    {
        var user = new ApplicationUser { Id = "admin", IsActive = true, IsDeleted = false };
        var users = CreateUserManager();
        var proof = new Mock<IAccountSecurityProofService>();
        var tokens = new Mock<IIdentityTokenStateService>();
        users.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        users.Setup(x => x.IsInRoleAsync(user, IdentityRoleNames.Superadmin)).ReturnsAsync(true);
        proof.Setup(x => x.IsValidAsync(user.Id, "invalid", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        tokens.Setup(x => x.IsEligible(user)).Returns(true);
        var service = new IdentityApiClientAdministrationService(users.Object, proof.Object, tokens.Object);

        var result = await service.ValidateAsync(user.Id, "invalid");

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, error => error.Metadata.ContainsKey("Validation"));
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
