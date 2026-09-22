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

public class RegenerateRecoveryCodesAccountServiceTest
{
    [Fact]
    public async Task RejectsRegenerationWhenTwoFactorAuthenticationIsDisabled()
    {
        var user = User(twoFactorEnabled: false);
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        var tokenState = new Mock<IIdentityTokenStateService>();
        tokenState.Setup(service => service.IsEligible(user)).Returns(true);
        var securityProof = new Mock<IAccountSecurityProofService>();
        var service = CreateService(userManager, tokenState, securityProof);

        var result = await service.RegenerateRecoveryCodesAsync(user.Id, "proof", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        securityProof.Verify(service => service.IsValidAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        userManager.Verify(manager => manager.GenerateNewTwoFactorRecoveryCodesAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RejectsRegenerationWhenStepUpProofIsInvalid()
    {
        var user = User(twoFactorEnabled: true);
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        var tokenState = new Mock<IIdentityTokenStateService>();
        tokenState.Setup(service => service.IsEligible(user)).Returns(true);
        var securityProof = new Mock<IAccountSecurityProofService>();
        securityProof.Setup(service => service.IsValidAsync(user.Id, "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(userManager, tokenState, securityProof);

        var result = await service.RegenerateRecoveryCodesAsync(user.Id, "proof", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        securityProof.Verify(service => service.IsValidAsync(user.Id, "proof", It.IsAny<CancellationToken>()), Times.Once);
        userManager.Verify(manager => manager.GenerateNewTwoFactorRecoveryCodesAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<int>()), Times.Never);
    }

    private static AccountService CreateService(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<IIdentityTokenStateService> tokenState,
        Mock<IAccountSecurityProofService> securityProof)
    {
        var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>().Options);
        return new AccountService(
            userManager.Object,
            CreateSignInManager(userManager.Object).Object,
            Mock.Of<IEmailSender>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<AccountService>>(),
            tokenState.Object,
            securityProof.Object,
            new IdentityUnitOfWork(context),
            context);
    }

    private static ApplicationUser User(bool twoFactorEnabled) => new()
    {
        Id = "user-1",
        Email = "user@example.test",
        IsActive = true,
        IsDeleted = false,
        TwoFactorEnabled = twoFactorEnabled
    };

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        return new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(UserManager<ApplicationUser> userManager)
    {
        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
    }
}
