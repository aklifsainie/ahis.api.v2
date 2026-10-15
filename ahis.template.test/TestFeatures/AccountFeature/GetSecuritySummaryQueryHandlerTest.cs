using ahis.template.application.Features.AccountFeatures.Queries;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class GetSecuritySummaryQueryHandlerTest
{
    [Fact]
    public async Task UsesTheAuthenticatedOwnerAndReturnsOnlyTheSecurityState()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetSecuritySummaryAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new SecuritySummaryDto
            {
                EmailConfirmed = true,
                PhoneConfirmed = false,
                PasswordPresent = true,
                TwoFactorEnabled = true,
                AuthenticatorConfigured = true,
                RemainingRecoveryCodeCount = 8,
                ActiveSessionCount = 2
            }));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new GetSecuritySummaryQueryHandler(
            accountService.Object,
            CurrentUser("user-1").Object,
            auditLogger.Object);

        var result = await handler.Handle(new GetSecuritySummaryQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmailConfirmed.Should().BeTrue();
        result.Value.PhoneConfirmed.Should().BeFalse();
        result.Value.PasswordPresent.Should().BeTrue();
        result.Value.TwoFactorEnabled.Should().BeTrue();
        result.Value.AuthenticatorConfigured.Should().BeTrue();
        result.Value.RemainingRecoveryCodeCount.Should().Be(8);
        result.Value.ActiveSessionCount.Should().Be(2);
        accountService.Verify(service => service.GetSecuritySummaryAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.View,
            "AccountSecurity",
            "user-1",
            "SecuritySummaryViewed",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectsMissingAuthenticatedUserWithoutCallingTheService()
    {
        var accountService = new Mock<IAccountService>();
        var handler = new GetSecuritySummaryQueryHandler(
            accountService.Object,
            CurrentUser(null).Object,
            Mock.Of<IAuditLogger>());

        var result = await handler.Handle(new GetSecuritySummaryQuery(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        accountService.Verify(service => service.GetSecuritySummaryAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<ICurrentUserService> CurrentUser(string? userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);
        return currentUser;
    }
}
