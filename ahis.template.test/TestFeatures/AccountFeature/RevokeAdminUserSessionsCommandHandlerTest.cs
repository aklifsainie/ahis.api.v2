using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Errors;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class RevokeAdminUserSessionsCommandHandlerTest
{
    [Fact]
    public async Task UsesTheAuthenticatedActorAndRouteTargetThenAuditsSuccess()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.RevokeAdminUserSessionsAsync(
                "actor-1", "target-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(true));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = CreateHandler(accountService.Object, CurrentUser("actor-1").Object, auditLogger.Object);

        var result = await handler.Handle(
            new RevokeAdminUserSessionsCommand { UserId = "target-1", StepUpProof = "proof" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        accountService.Verify(service => service.RevokeAdminUserSessionsAsync(
            "actor-1", "target-1", "proof", It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.StatusChange,
            "AccountSecurity",
            "target-1",
            "AdminSessionsRevoked",
            null,
            null,
            null,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RejectsMissingActorOrProofWithoutCallingTheService()
    {
        var accountService = new Mock<IAccountService>();
        var handler = CreateHandler(accountService.Object, CurrentUser(null).Object, Mock.Of<IAuditLogger>());

        var result = await handler.Handle(
            new RevokeAdminUserSessionsCommand { UserId = "target-1" },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ValidationError>();
        accountService.Verify(service => service.RevokeAdminUserSessionsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MapsAMissingTargetToNotFoundWithoutAuditing()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.RevokeAdminUserSessionsAsync(
                "actor-1", "missing", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(false));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = CreateHandler(accountService.Object, CurrentUser("actor-1").Object, auditLogger.Object);

        var result = await handler.Handle(
            new RevokeAdminUserSessionsCommand { UserId = "missing", StepUpProof = "proof" },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<EntityNotFoundError>();
        auditLogger.Verify(logger => logger.LogAsync(
            It.IsAny<AuditActionEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreservesCommittedSuccessWhenAuditDeliveryFails()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.RevokeAdminUserSessionsAsync(
                "actor-1", "target-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(true));
        var auditLogger = new Mock<IAuditLogger>();
        auditLogger.Setup(logger => logger.LogAsync(
                It.IsAny<AuditActionEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var handler = CreateHandler(accountService.Object, CurrentUser("actor-1").Object, auditLogger.Object);

        var result = await handler.Handle(
            new RevokeAdminUserSessionsCommand { UserId = "target-1", StepUpProof = "proof" },
            new CancellationToken(canceled: true));

        result.IsSuccess.Should().BeTrue();
    }

    private static RevokeAdminUserSessionsCommandHandler CreateHandler(
        IAccountService accountService,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger) =>
        new(accountService, currentUser, auditLogger, Mock.Of<ILogger<RevokeAdminUserSessionsCommandHandler>>());

    private static Mock<ICurrentUserService> CurrentUser(string? userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);
        return currentUser;
    }
}
