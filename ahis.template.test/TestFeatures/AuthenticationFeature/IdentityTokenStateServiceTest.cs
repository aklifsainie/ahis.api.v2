using ahis.template.identity.Contexts;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ahis.template.test.TestFeatures.AuthenticationFeature;

public class IdentityTokenStateServiceTest
{
    private static IdentityTokenStateService CreateService(ApplicationUser? user = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        manager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>().Options);
        return new IdentityTokenStateService(manager.Object, context);
    }

    private static ApplicationUser ActiveUser() => new()
    {
        Id = "user-1",
        IsActive = true,
        IsDeleted = false,
        SecurityStamp = "test-security-stamp"
    };

    [Fact]
    public async Task MatchingVersionForActiveUserIsAccepted()
    {
        var user = ActiveUser();
        var service = CreateService(user);

        (await service.ValidateAsync(user.Id, service.GetSecurityVersion(user))).Should().BeTrue();
    }

    [Fact]
    public async Task MissingUserOrVersionIsRejected()
    {
        var user = ActiveUser();
        var service = CreateService(user);

        (await service.ValidateAsync(user.Id, null)).Should().BeFalse();
        (await CreateService().ValidateAsync(user.Id, service.GetSecurityVersion(user))).Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void IneligibleAccountIsRejected(bool active, bool deleted, bool lockedOut)
    {
        var user = ActiveUser();
        user.IsActive = active;
        user.IsDeleted = deleted;
        user.LockoutEnabled = lockedOut;
        user.LockoutEnd = lockedOut ? DateTimeOffset.UtcNow.AddMinutes(5) : null;
        var service = CreateService(user);

        service.Matches(user, service.GetSecurityVersion(user)).Should().BeFalse();
    }

    [Fact]
    public void ChangedOrLegacyVersionIsRejected()
    {
        var user = ActiveUser();
        var service = CreateService(user);
        var previousVersion = service.GetSecurityVersion(user);
        user.SecurityStamp = "rotated-stamp";

        service.Matches(user, previousVersion).Should().BeFalse();
        service.Matches(user, null).Should().BeFalse();
        service.Matches(user, "not-hex").Should().BeFalse();
    }
}
