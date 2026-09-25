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

public class RevokeAdminUserSessionsAccountServiceTest
{
    [Fact]
    public async Task RejectsAnInvalidActorProofBeforeLookingUpTheTarget()
    {
        var userManager = CreateUserManager();
        var securityProof = new Mock<IAccountSecurityProofService>();
        securityProof.Setup(service => service.IsValidAsync(
                "actor-1", "proof-for-another-actor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(userManager, securityProof.Object);

        var result = await service.RevokeAdminUserSessionsAsync(
            "actor-1", "target-1", "proof-for-another-actor", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        securityProof.Verify(service => service.IsValidAsync(
            "actor-1", "proof-for-another-actor", It.IsAny<CancellationToken>()), Times.Once);
        userManager.Verify(manager => manager.FindByIdAsync("target-1"), Times.Never);
    }

    [Fact]
    public async Task ReturnsFalseForAMissingTargetOnlyAfterTheActorProofSucceeds()
    {
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync("missing-target")).ReturnsAsync((ApplicationUser?)null);
        var securityProof = new Mock<IAccountSecurityProofService>();
        securityProof.Setup(service => service.IsValidAsync("actor-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(userManager, securityProof.Object);

        var result = await service.RevokeAdminUserSessionsAsync(
            "actor-1", "missing-target", "proof", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        userManager.Verify(manager => manager.FindByIdAsync("missing-target"), Times.Once);
    }

    private static AccountService CreateService(
        Mock<UserManager<ApplicationUser>> userManager,
        IAccountSecurityProofService securityProof)
    {
        var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>().Options);
        return new AccountService(
            userManager.Object,
            CreateSignInManager(userManager.Object).Object,
            Mock.Of<IEmailSender>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<AccountService>>(),
            Mock.Of<IIdentityTokenStateService>(),
            securityProof,
            new IdentityUnitOfWork(context),
            context);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager() =>
        new(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

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
