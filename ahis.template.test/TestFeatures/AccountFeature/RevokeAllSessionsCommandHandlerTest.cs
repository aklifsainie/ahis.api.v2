using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class RevokeAllSessionsCommandHandlerTest
{
    [Fact]
    public async Task UsesTheAuthenticatedPrincipalAndAuditsSuccessfulRevocation()
    {
        var accountService = new Mock<IAccountService>();
        accountService
            .Setup(service => service.RevokeAllSessionsAsync("user-1", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new RevokeAllSessionsCommandHandler(
            accountService.Object,
            CurrentUser("user-1").Object,
            auditLogger.Object);

        var result = await handler.Handle(new RevokeAllSessionsCommand { StepUpProof = "proof" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        accountService.Verify(service => service.RevokeAllSessionsAsync("user-1", "proof", It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.Logout,
            "AccountSecurity",
            "user-1",
            "AllSessionsRevoked",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectsMissingAuthenticatedUserWithoutCallingTheService()
    {
        var accountService = new Mock<IAccountService>();
        var handler = new RevokeAllSessionsCommandHandler(
            accountService.Object,
            CurrentUser(null).Object,
            Mock.Of<IAuditLogger>());

        var result = await handler.Handle(new RevokeAllSessionsCommand { StepUpProof = "proof" }, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        accountService.Verify(service => service.RevokeAllSessionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<ICurrentUserService> CurrentUser(string? userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);
        return currentUser;
    }
}
