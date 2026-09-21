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

public class GetActiveSessionsQueryHandlerTest
{
    [Fact]
    public async Task UsesTheAuthenticatedOwnerAndReturnsOnlyTheServiceProjection()
    {
        var currentSession = Guid.NewGuid();
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetActiveSessionsAsync(
                "user-1", currentSession, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(((IReadOnlyList<ActiveSessionDto>)new List<ActiveSessionDto>
            {
                new()
                {
                    SessionId = currentSession,
                    CreatedAt = DateTime.UnixEpoch,
                    LastUsedAt = DateTime.UnixEpoch.AddMinutes(1),
                    ExpiresAt = DateTime.UnixEpoch.AddDays(1),
                    IsCurrent = true
                }
            }, 1)));
        var auditLogger = new Mock<IAuditLogger>();
        var handler = new GetActiveSessionsQueryHandler(
            accountService.Object,
            CurrentUser("user-1", currentSession).Object,
            auditLogger.Object);

        var result = await handler.Handle(new GetActiveSessionsQuery { PageNumber = 0, PageSize = 101 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Data.Should().ContainSingle(session => session.SessionId == currentSession && session.IsCurrent);
        accountService.Verify(service => service.GetActiveSessionsAsync(
            "user-1", currentSession, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
        auditLogger.Verify(logger => logger.LogAsync(
            AuditActionEnum.View,
            "AccountSecurity",
            "Sessions",
            "ActiveSessionCount=1",
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectsMissingAuthenticatedUserWithoutCallingTheService()
    {
        var accountService = new Mock<IAccountService>();
        var handler = new GetActiveSessionsQueryHandler(
            accountService.Object,
            CurrentUser(null, null).Object,
            Mock.Of<IAuditLogger>());

        var result = await handler.Handle(new GetActiveSessionsQuery(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        accountService.Verify(service => service.GetActiveSessionsAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<ICurrentUserService> CurrentUser(string? userId, Guid? sessionId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);
        currentUser.SetupGet(service => service.SessionId).Returns(sessionId);
        return currentUser;
    }
}
