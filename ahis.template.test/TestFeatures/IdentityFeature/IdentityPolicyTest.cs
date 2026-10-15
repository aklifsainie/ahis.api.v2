using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using ahis.template.identity.Services;
using ahis.template.identity.Contexts;
using ahis.template.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.DataProtection;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public sealed class IdentityPolicyTest
{
    [Fact]
    public async Task PasskeyServiceFailsClosedOutsideDevelopmentWithoutDatabaseAccess()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NeverConnect;Trusted_Connection=True").Options;
        using var context = new IdentityContext(options);
        var users = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!).Object;
        var service = new PasskeyService(context, users, Mock.Of<IAccountSecurityProofService>(), Mock.Of<IIdentityTokenStateService>(),
            Mock.Of<IIdentityRestrictionService>(), Mock.Of<IAuthenticationService>(), Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Production"),
            NullLogger<PasskeyService>.Instance, new EphemeralDataProtectionProvider());

        var result = await service.AuthenticationOptionsAsync("127.0.0.1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Message.Should().Be("Passkey operation failed.");
    }

    [Fact]
    public void PasskeyRoutesDeclareOwnerAuthorizationOrAnonymousAccessAndRateLimits()
    {
        var account = typeof(ahis.template.api.Controllers.v1.AccountController);
        var authentication = typeof(ahis.template.api.Controllers.v1.AuthenticationController);
        account.GetMethods().Where(x => x.Name.Contains("Passkey", StringComparison.Ordinal)).Should().NotBeEmpty();
        authentication.GetMethod("PasskeyOptions")!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Should().NotBeNull();
        authentication.GetMethod("CompletePasskey")!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Should().NotBeNull();
    }
    [Fact]
    public async Task PermissionCatalogAllowsCurrentSuperadminBearer()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var (token, _) = await host.AddUserAsync("admin", "Superadmin");
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/admin/identity/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, null, HttpStatusCode.Unauthorized)]
    [InlineData("user", null, HttpStatusCode.Forbidden)]
    [InlineData(null, "country-read", HttpStatusCode.Unauthorized)]
    [InlineData("admin", null, HttpStatusCode.OK)]
    public async Task PermissionCatalogEnforcesBearerSuperadmin(string? principal, string? apiKey, HttpStatusCode expected)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        using var client = host.CreateClient();
        if (principal is not null)
        {
            var (token, _) = await host.AddUserAsync(principal, principal == "admin" ? "Superadmin" : "User");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (apiKey is not null) client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        using var response = await client.GetAsync("/api/admin/identity/permissions");

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task RoleCatalogRequiresCurrentSuperadminBearer()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var (token, _) = await host.AddUserAsync("role-admin", "Superadmin");
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/admin/identity/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("country-read", "/api/v1/country", HttpStatusCode.OK)]
    [InlineData("country-read", "/api/v1/country/1", HttpStatusCode.OK)]
    [InlineData("country-write", "/api/v1/country", HttpStatusCode.Forbidden)]
    [InlineData("invalid", "/api/v1/country", HttpStatusCode.Unauthorized)]
    public async Task CountryReadPolicySelectsApiClientPermission(string key, string path, HttpStatusCode expected)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", key);

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task CountryBearerTakesPrecedenceOverValidApiKey()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        client.DefaultRequestHeaders.Add("X-API-Key", "country-read");

        using var response = await client.GetAsync("/api/v1/country");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("country-read", HttpStatusCode.Forbidden)]
    [InlineData("country-write", HttpStatusCode.NoContent)]
    public async Task CountryWritePolicySelectsApiClientPermission(string key, HttpStatusCode expected)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", key);

        using var response = await client.DeleteAsync("/api/v1/country/1");

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Superadmin", HttpStatusCode.NoContent)]
    public async Task CountryWritePolicySelectsBearerRole(string role, HttpStatusCode expected)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var (token, _) = await host.AddUserAsync($"write-{role}", role);
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.DeleteAsync("/api/v1/country/1");

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task RevokedSessionInvalidatesExistingBearer()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var (token, sessionId) = await host.AddUserAsync("revoked-admin", "Superadmin");
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var before = await client.GetAsync("/api/admin/identity/permissions");
        before.StatusCode.Should().Be(HttpStatusCode.OK);

        await host.RevokeSessionAsync(sessionId);
        using var after = await client.GetAsync("/api/admin/identity/permissions");

        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("country.read", HttpStatusCode.OK, HttpStatusCode.Forbidden)]
    [InlineData("country.write", HttpStatusCode.Forbidden, HttpStatusCode.NoContent)]
    public async Task CountryPoliciesApplyHumanPermissionClaim(string permission, HttpStatusCode read, HttpStatusCode write)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var token = await host.AddPermissionTokenAsync($"permission-{permission.Replace('.', '-')}", permission);
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var get = await client.GetAsync("/api/v1/country");
        using var delete = await client.DeleteAsync("/api/v1/country/1");

        get.StatusCode.Should().Be(read);
        delete.StatusCode.Should().Be(write);
    }

    [Theory]
    [InlineData("Superadmin", HttpStatusCode.OK)]
    [InlineData("User", HttpStatusCode.Forbidden)]
    public async Task UserRoleAssignmentReadRequiresSuperadmin(string role, HttpStatusCode expected)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var (token, _) = await host.AddUserAsync($"assignment-{role}", role);
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/admin/identity/users/target/roles");

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task LoginExcludesDirectReservedPermissionClaimAndIncludesAllowedRoleClaim()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        await host.AddPasswordUserAsync("direct-claim", directPermission: "country.write");
        await host.AddPasswordUserAsync("role-claim", rolePermission: "country.write");
        var directToken = await host.LoginAsync("direct-claim");
        var roleToken = await host.LoginAsync("role-claim");
        using var client = host.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directToken);
        using var directResponse = await client.DeleteAsync("/api/v1/country/1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", roleToken);
        using var roleResponse = await client.DeleteAsync("/api/v1/country/1");

        directResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        roleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangedRolePermissionInvalidatesOldBearerAndRemovesNewAuthorization()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        await host.AddPasswordUserAsync("permission-member", rolePermission: "country.write");
        await host.AddPasswordUserAsync("permission-admin");
        var adminId = await host.GetUserIdAsync("permission-admin");
        await host.AssignRoleAsync(adminId, IdentityRoleNames.Superadmin);
        var proof = await host.CreateProofAsync(adminId);
        var oldToken = await host.LoginAsync("permission-member");
        var roleId = await host.GetRoleIdAsync("Role-permission-member");
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        using var before = await client.DeleteAsync("/api/v1/country/1");
        before.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = host.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IIdentityRoleManagementService>();
            var tag = (await service.GetRoleAsync(roleId, CancellationToken.None)).Value.ETag;
            var changed = await service.ReplaceRolePermissionsAsync(adminId, proof, roleId, tag, [], CancellationToken.None);
            changed.IsSuccess.Should().BeTrue();
        }
        using var stale = await client.DeleteAsync("/api/v1/country/1");
        var newToken = await host.LoginAsync("permission-member");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        using var current = await client.DeleteAsync("/api/v1/country/1");

        stale.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        current.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PermissionCatalogAppliesAuthenticatedSecurityRateLimit()
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var (token, _) = await host.AddUserAsync("rate-admin", "Superadmin");
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var request = 0; request < 5; request++)
        {
            using var allowed = await client.GetAsync("/api/admin/identity/permissions");
            allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        using var limited = await client.GetAsync("/api/admin/identity/permissions");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Theory]
    [InlineData("POST", "/api/admin/identity/roles", "{\"name\":\"TestRole\"}")]
    [InlineData("PUT", "/api/admin/identity/roles/target/permissions", "{\"permissions\":[]}")]
    [InlineData("PUT", "/api/admin/identity/users/target/roles", "{\"roleIds\":[]}")]
    public async Task RoleMutationsDenyAnonymousOrdinaryUserAndApiKey(string method, string path, string body)
    {
        await using var host = new IdentityPolicyHost();
        await host.InitializeAsync();
        var (token, _) = await host.AddUserAsync("ordinary-role-reader", "User");
        using var client = host.CreateClient();

        using var anonymous = await SendAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var ordinary = await SendAsync();
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Add("X-API-Key", "country-write");
        using var apiKey = await SendAsync();

        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ordinary.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        apiKey.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        async Task<HttpResponseMessage> SendAsync()
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            return await client.SendAsync(request);
        }
    }
}
