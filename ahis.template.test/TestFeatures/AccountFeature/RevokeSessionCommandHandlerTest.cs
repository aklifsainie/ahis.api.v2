using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class RevokeSessionCommandHandlerTest
{
    [Fact]
    public async Task UsesTheAuthenticatedCurrentSessionAndAuditsSuccessfulRevocation()
    {
        var sessionId = Guid.NewGuid();
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.RevokeSessionAsync(
                "user-1", sessionId, sessionId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new RevokeSessionCommandHandler(
            accountService.Object,
            CurrentUser("user-1", sessionId).Object,
            auditLogger.Object);

        var result = await handler.Handle(
            new RevokeSessionCommand { SessionId = sessionId },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        accountService.Verify(service => service.RevokeSessionAsync(
            "user-1", sessionId, sessionId, null, It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.Logout,
            "AccountSecurity",
            "user-1",
            "SessionRevoked",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForwardsTheStepUpProofForAnotherSession()
    {
        var currentSessionId = Guid.NewGuid();
        var selectedSessionId = Guid.NewGuid();
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.RevokeSessionAsync(
                "user-1", selectedSessionId, currentSessionId, "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var handler = new RevokeSessionCommandHandler(
            accountService.Object,
            CurrentUser("user-1", currentSessionId).Object,
            Mock.Of<IAuditLogger>());

        var result = await handler.Handle(
            new RevokeSessionCommand { SessionId = selectedSessionId, StepUpProof = "proof" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        accountService.Verify(service => service.RevokeSessionAsync(
            "user-1", selectedSessionId, currentSessionId, "proof", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectsMissingAuthenticatedUserWithoutCallingTheService()
    {
        var accountService = new Mock<IAccountService>();
        var handler = new RevokeSessionCommandHandler(
            accountService.Object,
            CurrentUser(null, null).Object,
            Mock.Of<IAuditLogger>());

        var result = await handler.Handle(
            new RevokeSessionCommand { SessionId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        accountService.Verify(service => service.RevokeSessionAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<ICurrentUserService> CurrentUser(string? userId, Guid? sessionId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);
        currentUser.SetupGet(service => service.SessionId).Returns(sessionId);
        return currentUser;
    }
}
