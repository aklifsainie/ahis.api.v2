using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Services;
using ahis.template.identity.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using IdentityAuthenticationService = ahis.template.identity.Services.AuthenticationService;

namespace ahis.template.test.TestFeatures.AuthenticationFeature;

public class AuthenticationServiceTwoFactorChallengeTest
{
    [Fact]
    public async Task LoginWhenTwoFactorIsRequiredCreatesAnIdentityCompatibleChallengeAndReturnsEmailState()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            Email = "user@example.com",
            EmailConfirmed = true,
            IsActive = true,
            IsDeleted = false,
            TwoFactorEnabled = true,
            SecurityStamp = "security-stamp"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAuthentication()
            .AddCookie(IdentityConstants.TwoFactorUserIdScheme);
        services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options =>
        {
            options.Cookie.Path = "/api";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
        await using var serviceProvider = services.BuildServiceProvider();

        var loginContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByEmailAsync(user.UserName)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var signInManager = CreateSignInManager(userManager.Object, loginContext);
        signInManager
            .Setup(manager => manager.PasswordSignInAsync(user, "password", false, true))
            .ReturnsAsync(SignInResult.TwoFactorRequired);

        var tokenState = new Mock<IIdentityTokenStateService>();
        tokenState.Setup(service => service.IsEligible(user)).Returns(true);
        tokenState.Setup(service => service.GetSecurityVersion(user)).Returns("security-version");

        var authenticationService = CreateAuthenticationService(
            userManager.Object,
            signInManager.Object,
            tokenState.Object,
            loginContext);

        var login = await authenticationService.LoginAsync(user.UserName, "password");

        login.IsSuccess.Should().BeTrue();
        login.Value.RequiresTwoFactor.Should().BeTrue();
        login.Value.IsEmailConfirmed.Should().BeTrue();

        var setCookie = loginContext.Response.Headers.SetCookie.Single();
        setCookie.Should().Contain("path=/api");

        var cookieValue = setCookie.Split(';')[0].Split('=', 2)[1];
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.TwoFactorUserIdScheme);
        var ticket = options.TicketDataFormat.Unprotect(cookieValue);

        ticket.Should().NotBeNull();
        var principal = ticket!.Principal;
        principal.FindFirstValue(ClaimTypes.Name).Should().Be(user.Id);
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(user.Id);
        principal.FindFirstValue(IIdentityTokenStateService.SecurityVersionClaim).Should().Be("security-version");
    }

    private static IdentityAuthenticationService CreateAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityTokenStateService tokenState,
        HttpContext context)
    {
        var identityContext = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>().Options);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:AccessTokenExpirySeconds"] = "3600",
            ["Jwt:RefreshTokenExpiryDays"] = "30"
        }).Build();

        var restrictions = new Mock<IIdentityRestrictionService>();
        restrictions.Setup(service => service.IsAuthenticationAllowedAsync(It.IsAny<ApplicationUser?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new IdentityAuthenticationService(
            userManager,
            signInManager,
            identityContext,
            Mock.Of<IEmailSender>(),
            new IdentityUnitOfWork(identityContext),
            configuration,
            Mock.Of<ILogger<IdentityAuthenticationService>>(),
            tokenState,
            restrictions.Object,
            new HttpContextAccessor { HttpContext = context });
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

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(
        UserManager<ApplicationUser> userManager,
        HttpContext context)
    {
        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            new HttpContextAccessor { HttpContext = context },
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
    }
}
