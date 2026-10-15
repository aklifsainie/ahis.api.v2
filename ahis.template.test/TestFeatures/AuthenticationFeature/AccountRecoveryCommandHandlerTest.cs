using ahis.template.application.Features.AuthenticationFeatures.Commands;
using ahis.template.application.Interfaces.Commons;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ahis.template.test.TestFeatures.AuthenticationFeature;

public class AccountRecoveryCommandHandlerTest
{
    [Fact]
    public async Task StartUsesGenericAuditWithoutRecordingEmailMaterial()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(service => service.StartAccountRecoveryAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new StartAccountRecoveryCommandHandler(authenticationService.Object, auditLogger.Object);

        var result = await handler.Handle(new StartAccountRecoveryCommand { Email = "person@example.test" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.Update,
            "AccountRecovery",
            "pre-auth",
            "RecoveryStarted",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            It.IsAny<AuditActionEnum>(),
            It.IsAny<string>(),
            "person@example.test",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletePassesMfaEvidenceAndAuditsFailureWithoutSecrets()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(service => service.CompleteAccountRecoveryAsync(
                "challenge", "NewPassword1!", TwoFactorProviderEnum.RecoveryCode, "code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Invalid recovery request."));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new CompleteAccountRecoveryCommandHandler(authenticationService.Object, auditLogger.Object);

        var result = await handler.Handle(new CompleteAccountRecoveryCommand
        {
            Challenge = "challenge",
            NewPassword = "NewPassword1!",
            TwoFactorProvider = TwoFactorProviderEnum.RecoveryCode,
            TwoFactorCode = "code"
        }, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.LoginFailed,
            "AccountRecovery",
            "pre-auth",
            "RecoveryCompletionFailed",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
