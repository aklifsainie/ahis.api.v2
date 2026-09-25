using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Services;
using ahis.template.identity.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class UnlockAdminUserAccountServiceTest
{
    [Fact]
    public async Task RejectsAnActorWithoutCurrentSuperadminMembershipBeforeProofOrTargetLookup()
    {
        var actor = new ApplicationUser { Id = "actor-1" };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(actor.Id)).ReturnsAsync(actor);
        userManager.Setup(manager => manager.IsInRoleAsync(actor, "Superadmin")).ReturnsAsync(false);
        var proof = new Mock<IAccountSecurityProofService>();
        var service = CreateService(userManager, proof.Object);

        var result = await service.UnlockAdminUserAsync(
            actor.Id, "target-1", "proof", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        proof.Verify(service => service.IsValidAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        userManager.Verify(manager => manager.FindByIdAsync("target-1"), Times.Never);
    }

    [Fact]
    public async Task RejectsAnInvalidActorProofBeforeTargetLookup()
    {
        var actor = new ApplicationUser { Id = "actor-1" };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(actor.Id)).ReturnsAsync(actor);
        userManager.Setup(manager => manager.IsInRoleAsync(actor, "Superadmin")).ReturnsAsync(true);
        var proof = new Mock<IAccountSecurityProofService>();
        proof.Setup(service => service.IsValidAsync(actor.Id, "wrong-proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(userManager, proof.Object);

        var result = await service.UnlockAdminUserAsync(
            actor.Id, "target-1", "wrong-proof", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        userManager.Verify(manager => manager.FindByIdAsync("target-1"), Times.Never);
    }

    private static AccountService CreateService(
        Mock<UserManager<ApplicationUser>> userManager,
        IAccountSecurityProofService proof)
    {
        var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>().Options);
        return new AccountService(
            userManager.Object,
            CreateSignInManager(userManager.Object).Object,
            Mock.Of<IEmailSender>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<AccountService>>(),
            Mock.Of<IIdentityTokenStateService>(),
            proof,
            Mock.Of<IIdentityRestrictionService>(),
            new IdentityUnitOfWork(context),
            context);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager() =>
        new(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(UserManager<ApplicationUser> userManager) =>
        new(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
}
