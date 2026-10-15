using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ahis.template.test.TestFeatures.AccountFeature;

public sealed class AdminUserAccessCommandHandlerTest
{
    [Fact]
    public async Task DisableAuditsOnlyACommittedStateChange()
    {
        var accounts = new Mock<IAccountService>();
        accounts.Setup(x => x.DisableAdminUserAsync("actor", "target", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(AdminUserControlOutcome.Changed));
        var audit = new Mock<IAuditLogger>();
        var handler = Create(accounts.Object, audit.Object);

        var result = await handler.Handle(new DisableAdminUserCommand { UserId = "target", StepUpProof = "proof" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        audit.Verify(x => x.LogAsync(AuditActionEnum.StatusChange, "AccountSecurity", "target", "AdminUserDisabled", null, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task EnableDoesNotAuditAnIdempotentResult()
    {
        var accounts = new Mock<IAccountService>();
        accounts.Setup(x => x.EnableAdminUserAsync("actor", "target", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(AdminUserControlOutcome.AlreadyInRequestedState));
        var audit = new Mock<IAuditLogger>();
        var result = await Create(accounts.Object, audit.Object).Handle(new EnableAdminUserCommand { UserId = "target", StepUpProof = "proof" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetMfaUsesActorAndAuditsFixedActionAfterCommittedChange()
    {
        var accounts = new Mock<IAccountService>();
        accounts.Setup(x => x.ResetAdminUserMfaAsync("actor", "target", "proof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(AdminUserControlOutcome.Changed));
        var audit = new Mock<IAuditLogger>();

        var result = await Create(accounts.Object, audit.Object).Handle(
            new ResetAdminUserMfaCommand { UserId = "target", StepUpProof = "proof" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        audit.Verify(x => x.LogAsync(AuditActionEnum.StatusChange, "AccountSecurity", "target", "AdminUserMfaReset", null, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PasswordResetRequirementRejectsSelfActionBeforeCallingIdentityService()
    {
        var accounts = new Mock<IAccountService>();
        var audit = new Mock<IAuditLogger>();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UserId).Returns("target");
        var handler = new AdminUserAccessCommandHandler(accounts.Object, current.Object, audit.Object, Mock.Of<ILogger<AdminUserAccessCommandHandler>>());

        var result = await handler.Handle(new RequireAdminUserPasswordResetCommand { UserId = "target", StepUpProof = "proof" }, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        accounts.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
    }

    private static AdminUserAccessCommandHandler Create(IAccountService accounts, IAuditLogger audit)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UserId).Returns("actor");
        return new AdminUserAccessCommandHandler(accounts, current.Object, audit, Mock.Of<ILogger<AdminUserAccessCommandHandler>>());
    }
}
