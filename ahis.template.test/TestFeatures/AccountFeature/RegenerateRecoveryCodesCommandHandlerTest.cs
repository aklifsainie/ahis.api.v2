using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class RegenerateRecoveryCodesCommandHandlerTest
{
    [Fact]
    public async Task UsesTheAuthenticatedPrincipalAndAuditsSuccessfulRegeneration()
    {
        var codes = new[] { "code-1", "code-2" };
        var accountService = new Mock<IAccountService>();
        accountService
            .Setup(service => service.RegenerateRecoveryCodesAsync("user-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IEnumerable<string>>(codes));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new RegenerateRecoveryCodesCommandHandler(
            accountService.Object,
            CurrentUser("user-1").Object,
            auditLogger.Object);

        var result = await handler.Handle(
            new RegenerateRecoveryCodesCommand { StepUpProof = "proof" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(codes);
        accountService.Verify(service => service.RegenerateRecoveryCodesAsync(
            "user-1", "proof", It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.Update,
            "AccountSecurity",
            "user-1",
            "RecoveryCodesRegenerated",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuditsFailureWithoutIncludingRecoveryCodeMaterial()
    {
        var accountService = new Mock<IAccountService>();
        accountService
            .Setup(service => service.RegenerateRecoveryCodesAsync("user-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<IEnumerable<string>>("Unable to regenerate recovery codes."));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new RegenerateRecoveryCodesCommandHandler(
            accountService.Object,
            CurrentUser("user-1").Object,
            auditLogger.Object);

        var result = await handler.Handle(
            new RegenerateRecoveryCodesCommand { StepUpProof = "proof" },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.LoginFailed,
            "AccountSecurity",
            "user-1",
            "RecoveryCodesRegenerationFailed",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectsMissingAuthenticatedUserWithoutCallingTheServiceOrAuditLogger()
    {
        var accountService = new Mock<IAccountService>();
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new RegenerateRecoveryCodesCommandHandler(
            accountService.Object,
            CurrentUser(null).Object,
            auditLogger.Object);

        var result = await handler.Handle(
            new RegenerateRecoveryCodesCommand { StepUpProof = "proof" },
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        accountService.Verify(service => service.RegenerateRecoveryCodesAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        auditLogger.VerifyNoOtherCalls();
    }

    private static Mock<ICurrentUserService> CurrentUser(string? userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);
        return currentUser;
    }
}
