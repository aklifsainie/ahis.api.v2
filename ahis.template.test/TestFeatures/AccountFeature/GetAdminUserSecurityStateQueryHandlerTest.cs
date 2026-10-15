using ahis.template.application.Features.AccountFeatures.Queries;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Shared.Errors;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class GetAdminUserSecurityStateQueryHandlerTest
{
    [Fact]
    public async Task ReturnsTheApprovedSecurityStateForTheRouteTargetAndAuditsTheRead()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAdminUserSecurityStateAsync("target-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new AdminUserSecurityStateDto
            {
                UserId = "target-1",
                IsActive = true,
                IsDeleted = false,
                IsLockedOut = true,
                EmailConfirmed = true,
                PhoneConfirmed = false,
                PasswordPresent = true,
                TwoFactorEnabled = true,
                AuthenticatorConfigured = true,
                RemainingRecoveryCodeCount = 8,
                ActiveSessionCount = 2
            }));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new GetAdminUserSecurityStateQueryHandler(accountService.Object, auditLogger.Object);

        var result = await handler.Handle(
            new GetAdminUserSecurityStateQuery { UserId = "target-1" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be("target-1");
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.IsLockedOut.Should().BeTrue();
        result.Value.EmailConfirmed.Should().BeTrue();
        result.Value.PhoneConfirmed.Should().BeFalse();
        result.Value.PasswordPresent.Should().BeTrue();
        result.Value.TwoFactorEnabled.Should().BeTrue();
        result.Value.AuthenticatorConfigured.Should().BeTrue();
        result.Value.RemainingRecoveryCodeCount.Should().Be(8);
        result.Value.ActiveSessionCount.Should().Be(2);
        accountService.Verify(service => service.GetAdminUserSecurityStateAsync("target-1", It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.View,
            "AccountSecurity",
            "target-1",
            "AdminSecurityStateViewed",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MapsAMissingTargetToNotFoundWithoutAuditingTheRead()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAdminUserSecurityStateAsync("missing-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<AdminUserSecurityStateDto>("User not found."));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new GetAdminUserSecurityStateQueryHandler(accountService.Object, auditLogger.Object);

        var result = await handler.Handle(
            new GetAdminUserSecurityStateQuery { UserId = "missing-user" },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<EntityNotFoundError>();
        auditLogger.Verify(logger => logger.LogAsync(
            It.IsAny<AuditActionEnum>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
