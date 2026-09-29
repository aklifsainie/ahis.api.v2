using System.Text.Json;
using ahis.template.application.Features.IdentityRoleManagementFeatures;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public sealed class IdentityRoleManagementHandlerTest
{
    [Fact]
    public async Task AuditsSortedChangedRoleIdsAfterSuccessfulReplacement()
    {
        var service = new Mock<IIdentityRoleManagementService>();
        service.Setup(x => x.ReplaceUserRolesAsync("actor", "proof", "target", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new IdentityRoleAssignmentChangeDto(true, ["role-b", "role-c"], ["role-a"])));
        var audit = new Mock<IAuditLogger>();
        var handler = CreateHandler(service.Object, audit.Object);
        var expectedAdded = new[] { "role-b", "role-c" };
        var expectedRemoved = new[] { "role-a" };

        var result = await handler.Handle(new ReplaceUserRolesCommand
        {
            UserId = "target",
            RoleIds = ["role-b", "role-c"],
            StepUpProof = "proof"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        audit.Verify(x => x.LogAsync(
            ahis.template.domain.Enums.AuditActionEnum.Update,
            "IdentityRoleManagement",
            "target",
            It.Is<string>(metadata => HasExpectedRoleDelta(metadata, expectedAdded, expectedRemoved)),
            null, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DoesNotAuditNoOpOrFailedReplacement()
    {
        var service = new Mock<IIdentityRoleManagementService>();
        service.SetupSequence(x => x.ReplaceUserRolesAsync("actor", "proof", "target", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new IdentityRoleAssignmentChangeDto(false, [], [])))
            .ReturnsAsync(Result.Fail<IdentityRoleAssignmentChangeDto>(new Error("Invalid").WithMetadata("Validation", true)));
        var audit = new Mock<IAuditLogger>();
        var handler = CreateHandler(service.Object, audit.Object);

        (await handler.Handle(new ReplaceUserRolesCommand { UserId = "target", StepUpProof = "proof" }, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await handler.Handle(new ReplaceUserRolesCommand { UserId = "target", StepUpProof = "proof" }, CancellationToken.None)).IsFailed.Should().BeTrue();

        audit.Verify(x => x.LogAsync(
            It.IsAny<ahis.template.domain.Enums.AuditActionEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreservesCommittedSuccessWhenRoleReplacementAuditFails()
    {
        var service = new Mock<IIdentityRoleManagementService>();
        service.Setup(x => x.ReplaceUserRolesAsync("actor", "proof", "target", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new IdentityRoleAssignmentChangeDto(true, ["role-b"], [])));
        var audit = new Mock<IAuditLogger>();
        audit.Setup(x => x.LogAsync(
                It.IsAny<ahis.template.domain.Enums.AuditActionEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var handler = CreateHandler(service.Object, audit.Object);

        var result = await handler.Handle(new ReplaceUserRolesCommand { UserId = "target", StepUpProof = "proof" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private static IdentityRoleManagementHandler CreateHandler(IIdentityRoleManagementService service, IAuditLogger audit)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UserId).Returns("actor");
        return new IdentityRoleManagementHandler(service, current.Object, audit, Mock.Of<ILogger<IdentityRoleManagementHandler>>());
    }

    private static bool HasExpectedRoleDelta(string metadata, string[] added, string[] removed)
    {
        using var document = JsonDocument.Parse(metadata);
        return document.RootElement.GetProperty("action").GetString() == "UserRolesReplaced"
            && document.RootElement.GetProperty("addedRoleIds").EnumerateArray().Select(x => x.GetString()).SequenceEqual(added)
            && document.RootElement.GetProperty("removedRoleIds").EnumerateArray().Select(x => x.GetString()).SequenceEqual(removed)
            && !metadata.Contains("proof", StringComparison.OrdinalIgnoreCase);
    }
}
