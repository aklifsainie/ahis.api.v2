using ahis.template.identity.Interfaces;
using ahis.template.identity.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ahis.template.identity.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public sealed class IdentityConcurrencyTest
{
    [Fact]
    public async Task ConcurrentCrossDemotionPreservesAnEffectiveSuperadmin()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        await host.AddUserAsync("admin-one", IdentityRoleNames.Superadmin);
        await host.AddUserAsync("admin-two", IdentityRoleNames.Superadmin);
        var firstId = await host.GetUserIdAsync("admin-one");
        var secondId = await host.GetUserIdAsync("admin-two");
        await host.AssignRoleAsync(firstId, IdentityRoleNames.Superadmin);
        await host.AssignRoleAsync(secondId, IdentityRoleNames.Superadmin);
        var firstProof = await host.CreateProofAsync(firstId);
        var secondProof = await host.CreateProofAsync(secondId);
        using var firstScope = host.Services.CreateScope();
        using var secondScope = host.Services.CreateScope();
        var firstService = firstScope.ServiceProvider.GetRequiredService<IIdentityRoleManagementService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<IIdentityRoleManagementService>();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = Task.Run(async () => { await ready.Task; return await firstService.ReplaceUserRolesAsync(firstId, firstProof, secondId, [], CancellationToken.None); });
        var second = Task.Run(async () => { await ready.Task; return await secondService.ReplaceUserRolesAsync(secondId, secondProof, firstId, [], CancellationToken.None); });
        ready.SetResult();
        var results = await Task.WhenAll(first, second);

        results.Count(result => result.IsSuccess).Should().Be(1);
        (await host.CountUsersInRoleAsync(IdentityRoleNames.Superadmin)).Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshCannotLeaveAnActiveSessionAfterConcurrentRevocation(bool revokeAll)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        await host.AddPasswordUserAsync("race-user");
        var login = await host.LoginCredentialsAsync("race-user");
        var userId = login.UserId;
        var sessionId = await host.GetCurrentSessionIdAsync(userId);
        var proof = await host.CreateProofAsync(userId);
        using var revokeScope = host.Services.CreateScope();
        using var refreshScope = host.Services.CreateScope();
        var account = revokeScope.ServiceProvider.GetRequiredService<IAccountService>();
        var authentication = refreshScope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var revoke = Task.Run(async () =>
        {
            await ready.Task;
            return revokeAll
                ? await account.RevokeAllSessionsAsync(userId, proof, CancellationToken.None)
                : await account.RevokeSessionAsync(userId, sessionId, sessionId, null, CancellationToken.None);
        });
        var refresh = Task.Run(async () => { await ready.Task; return await authentication.RefreshTokenAsync(login.RefreshToken, CancellationToken.None); });
        ready.SetResult();
        var revoked = await revoke;
        var rotated = await refresh;

        revoked.IsSuccess.Should().BeTrue();
        await using var inspectScope = host.Services.CreateAsyncScope();
        var context = inspectScope.ServiceProvider.GetRequiredService<IdentityContext>();
        (await context.RefreshSessions.AnyAsync(session => session.UserId == userId && !session.IsRevoked)).Should().BeFalse();
        (await context.RefreshTokens.AnyAsync(token => token.UserId == userId && !token.IsRevoked)).Should().BeFalse();
        if (rotated.IsSuccess)
        {
            using var client = host.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", rotated.Value.AccessToken);
            using var response = await client.GetAsync("/api/admin/identity/permissions");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task ConcurrentPermissionChangeAndDemotionPreserveCommittedAuthorizationState()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        await host.AddPasswordUserAsync("permission-actor");
        await host.AddPasswordUserAsync("demotion-actor", rolePermission: "country.read");
        var firstId = await host.GetUserIdAsync("permission-actor");
        var secondId = await host.GetUserIdAsync("demotion-actor");
        await host.AssignRoleAsync(firstId, IdentityRoleNames.Superadmin);
        await host.AssignRoleAsync(secondId, IdentityRoleNames.Superadmin);
        var firstProof = await host.CreateProofAsync(firstId);
        var secondProof = await host.CreateProofAsync(secondId);
        var roleId = await host.GetRoleIdAsync("Role-demotion-actor");
        using var firstScope = host.Services.CreateScope();
        using var secondScope = host.Services.CreateScope();
        var firstService = firstScope.ServiceProvider.GetRequiredService<IIdentityRoleManagementService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<IIdentityRoleManagementService>();
        var tag = (await firstService.GetRoleAsync(roleId, CancellationToken.None)).Value.ETag;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var permissions = Task.Run(async () => { await ready.Task; return await firstService.ReplaceRolePermissionsAsync(firstId, firstProof, roleId, tag, [], CancellationToken.None); });
        var demotion = Task.Run(async () => { await ready.Task; return await secondService.ReplaceUserRolesAsync(secondId, secondProof, firstId, [], CancellationToken.None); });
        ready.SetResult();
        var changed = await permissions;
        var removed = await demotion;

        (await host.CountUsersInRoleAsync(IdentityRoleNames.Superadmin)).Should().BeGreaterThanOrEqualTo(1);
        if (changed.IsSuccess && changed.Value.Changed)
        {
            await using var inspectScope = host.Services.CreateAsyncScope();
            var context = inspectScope.ServiceProvider.GetRequiredService<IdentityContext>();
            (await context.RefreshSessions.AnyAsync(session => session.UserId == secondId && !session.IsRevoked)).Should().BeFalse();
            (await firstService.GetRolePermissionsAsync(roleId, CancellationToken.None)).Value.Should().BeEmpty();
        }
        if (removed.IsSuccess && removed.Value.Changed)
            (await host.CountUsersInRoleAsync(IdentityRoleNames.Superadmin)).Should().Be(1);
    }

    [Fact]
    public async Task StaleRoleTagCannotOverwriteCommittedRename()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        await host.AddPasswordUserAsync("etag-admin");
        var adminId = await host.GetUserIdAsync("etag-admin");
        await host.AssignRoleAsync(adminId, IdentityRoleNames.Superadmin);
        var proof = await host.CreateProofAsync(adminId);
        var roleId = await host.CreateRoleAsync("InitialRole");
        using var scope = host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IIdentityRoleManagementService>();
        var staleTag = (await service.GetRoleAsync(roleId, CancellationToken.None)).Value.ETag;

        var first = await service.RenameRoleAsync(adminId, proof, roleId, staleTag, "RenamedRole", CancellationToken.None);
        var second = await service.RenameRoleAsync(adminId, proof, roleId, staleTag, "OverwrittenRole", CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsFailed.Should().BeTrue();
        second.Errors.Single().Metadata.Should().ContainKey("PreconditionFailed");
        (await service.GetRoleAsync(roleId, CancellationToken.None)).Value.Name.Should().Be("RenamedRole");
    }

    [Fact]
    public async Task RepeatedOwnerSessionRevocationIsIdempotent()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        await host.AddPasswordUserAsync("repeat-revoke");
        var login = await host.LoginCredentialsAsync("repeat-revoke");
        var sessionId = await host.GetCurrentSessionIdAsync(login.UserId);
        using var scope = host.Services.CreateScope();
        var account = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var first = await account.RevokeSessionAsync(login.UserId, sessionId, sessionId, null, CancellationToken.None);
        var second = await account.RevokeSessionAsync(login.UserId, sessionId, sessionId, null, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        await using var inspectScope = host.Services.CreateAsyncScope();
        var context = inspectScope.ServiceProvider.GetRequiredService<IdentityContext>();
        (await context.RefreshSessions.AnyAsync(session => session.UserId == login.UserId && !session.IsRevoked)).Should().BeFalse();
        (await context.RefreshTokens.AnyAsync(token => token.UserId == login.UserId && !token.IsRevoked)).Should().BeFalse();
    }
}
