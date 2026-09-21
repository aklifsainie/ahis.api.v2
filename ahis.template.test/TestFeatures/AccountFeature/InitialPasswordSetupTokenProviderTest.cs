using ahis.template.identity.Models.Entities;
using ahis.template.identity.Services;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class InitialPasswordSetupTokenProviderTest
{
    [Fact]
    public async Task TokenIsBoundToItsPurposeAndCurrentSecurityStamp()
    {
        var user = new ApplicationUser { Id = "user-1", SecurityStamp = "stamp-1" };
        var manager = CreateUserManager(user);
        var provider = new InitialPasswordSetupTokenProvider(
            new EphemeralDataProtectionProvider(),
            Options.Create(new InitialPasswordSetupTokenProviderOptions()),
            NullLogger<DataProtectorTokenProvider<ApplicationUser>>.Instance);

        var token = await provider.GenerateAsync(
            InitialPasswordSetupTokenProvider.Purpose,
            manager.Object,
            user);

        (await provider.ValidateAsync(
            InitialPasswordSetupTokenProvider.Purpose,
            token,
            manager.Object,
            user)).Should().BeTrue();

        manager.Setup(service => service.GetSecurityStampAsync(user)).ReturnsAsync("stamp-2");

        (await provider.ValidateAsync(
            InitialPasswordSetupTokenProvider.Purpose,
            token,
            manager.Object,
            user)).Should().BeFalse();
        (await provider.ValidateAsync(
            "different-purpose",
            token,
            manager.Object,
            user)).Should().BeFalse();
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager(ApplicationUser user)
    {
        var store = new Mock<IUserSecurityStampStore<ApplicationUser>>();
        store.Setup(service => service.GetUserIdAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.Id);
        store.Setup(service => service.GetSecurityStampAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.SecurityStamp!);

        var manager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        manager.Setup(service => service.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        manager.Setup(service => service.GetSecurityStampAsync(user)).ReturnsAsync(user.SecurityStamp!);
        manager.SetupGet(service => service.SupportsUserSecurityStamp).Returns(true);
        return manager;
    }
}
