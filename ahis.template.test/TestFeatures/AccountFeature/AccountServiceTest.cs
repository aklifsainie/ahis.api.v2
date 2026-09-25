using ahis.template.identity.Interfaces;
using ahis.template.identity.Contexts;
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

public class AccountServiceTest
{
    [Fact]
    public async Task GenerateAuthenticatorSetupAsync_RejectsResetWhenTwoFactorIsEnabled()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "user@example.test",
            TwoFactorEnabled = true
        };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>().Options);
        var service = new AccountService(
            userManager.Object,
            CreateSignInManager(userManager.Object).Object,
            Mock.Of<IEmailSender>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<AccountService>>(),
            Mock.Of<IIdentityTokenStateService>(),
            Mock.Of<IAccountSecurityProofService>(),
            Mock.Of<IIdentityRestrictionService>(),
            new IdentityUnitOfWork(context),
            context);

        var result = await service.GenerateAuthenticatorSetupAsync(user.Id);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error =>
            error.Message.Contains("already enabled", StringComparison.Ordinal));
        userManager.Verify(manager => manager.ResetAuthenticatorKeyAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

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
