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

public class UnlockAdminUserCommandHandlerTest
{
    [Fact]
    public async Task AuditsOnlyACommittedUnlock()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.UnlockAdminUserAsync(
                "actor-1", "target-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(AdminUserUnlockOutcome.Unlocked));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = CreateHandler(accountService.Object, "actor-1", auditLogger.Object);

        var result = await handler.Handle(
            new UnlockAdminUserCommand { UserId = "target-1", StepUpProof = "proof" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(AdminUserUnlockOutcome.Unlocked);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.StatusChange, "AccountSecurity", "target-1", "AdminUserUnlocked",
            null, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DoesNotAuditACleanAlreadyUnlockedTarget()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.UnlockAdminUserAsync(
                "actor-1", "target-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(AdminUserUnlockOutcome.AlreadyUnlocked));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = CreateHandler(accountService.Object, "actor-1", auditLogger.Object);

        var result = await handler.Handle(
            new UnlockAdminUserCommand { UserId = "target-1", StepUpProof = "proof" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(AdminUserUnlockOutcome.AlreadyUnlocked);
        auditLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RejectsSelfUnlockWithoutCallingTheService()
    {
        var accountService = new Mock<IAccountService>();
        var handler = CreateHandler(accountService.Object, "actor-1", Mock.Of<IAuditLogger>());

        var result = await handler.Handle(
            new UnlockAdminUserCommand { UserId = "actor-1", StepUpProof = "proof" },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ValidationError>();
        accountService.Verify(service => service.UnlockAdminUserAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MapsOnlyASuccessfulMissingTargetOutcomeToNotFound()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.UnlockAdminUserAsync(
                "actor-1", "missing", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(AdminUserUnlockOutcome.TargetNotFound));
        var handler = CreateHandler(accountService.Object, "actor-1", Mock.Of<IAuditLogger>());

        var result = await handler.Handle(
            new UnlockAdminUserCommand { UserId = "missing", StepUpProof = "proof" },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<EntityNotFoundError>();
    }

    private static UnlockAdminUserCommandHandler CreateHandler(
        IAccountService accountService,
        string? userId,
        IAuditLogger auditLogger)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);
        return new UnlockAdminUserCommandHandler(
            accountService, currentUser.Object, auditLogger, Mock.Of<ILogger<UnlockAdminUserCommandHandler>>());
    }
}
