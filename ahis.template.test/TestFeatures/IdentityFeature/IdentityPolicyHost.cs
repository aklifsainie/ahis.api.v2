using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ahis.template.api;
using ahis.template.application.Features.CountryFeatures.Command;
using ahis.template.application.Features.CountryFeatures.Query;
using ahis.template.application.Features.IdentityRoleManagementFeatures;
using ahis.template.application.Interfaces.Validators;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using ahis.template.domain.Models.ViewModels.CountryVM;
using ahis.template.domain.Models.ViewModels.IdentityRoleVM;
using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace ahis.template.test.TestFeatures.IdentityFeature;

internal sealed class IdentityPolicyHost : WebApplicationFactory<Program>, IAsyncDisposable
{
    private const string Issuer = "ahis-policy-test";
    private const string Audience = "ahis-policy-test-client";
    private const string SigningKey = "identity-policy-tests-use-only-this-unique-key-2026";
    private readonly string _databaseName = $"AhisIdentityPolicy_{Guid.NewGuid():N}";
    private bool _created;
    private string ConnectionString => $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Authentication:Lockout:DefaultLockoutTimeSpan", "00:05:00");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:SigningKey"] = SigningKey,
            ["Jwt:AccessTokenExpirySeconds"] = "3600",
            ["Authentication:Lockout:DefaultLockoutTimeSpan"] = "00:05:00",
            ["Authentication:Lockout:MaxFailedAccessAttempts"] = "3",
            ["Identity:Bootstrap:SuperadminEmail"] = ""
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIdentityRoleBootstrapService>();
            var bootstrap = new Mock<IIdentityRoleBootstrapService>();
            services.AddSingleton(bootstrap.Object);
            services.RemoveAll<IMediator>();
            services.AddSingleton<IMediator, PolicyMediator>();
            services.RemoveAll<IApiKeyValidator>();
            services.AddSingleton<IApiKeyValidator, PolicyApiKeyValidator>();
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    public async Task InitializeAsync()
    {
        _ = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        await context.Database.EnsureCreatedAsync();
        _created = true;
    }

    public async Task<(string Token, Guid SessionId)> AddUserAsync(string name, params string[] roles)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString("N"), UserName = name, Email = $"{name}@example.test", EmailConfirmed = true };
        context.Users.Add(user);
        var session = new RefreshSession { PublicId = Guid.NewGuid(), UserId = user.Id, CreatedAt = DateTime.UtcNow, LastUsedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(1) };
        context.RefreshSessions.Add(session);
        await context.SaveChangesAsync();
        var version = scope.ServiceProvider.GetRequiredService<IIdentityTokenStateService>().GetSecurityVersion(user)!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(IIdentityTokenStateService.SecurityVersionClaim, version),
            new(IIdentityTokenStateService.TokenUseClaim, IIdentityTokenStateService.AccessTokenUse),
            new("session_id", session.PublicId.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), session.PublicId);
    }

    public async Task<string> AddPermissionTokenAsync(string name, string permission)
    {
        var (token, _) = await AddUserAsync(name);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = jwt.Claims.Select(c => new Claim(c.Type, c.Value)).Append(new Claim("human_permission", permission));
        var signed = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(signed);
    }

    public async Task RevokeSessionAsync(Guid sessionId)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        await context.RefreshSessions.Where(s => s.PublicId == sessionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsRevoked, true).SetProperty(s => s.RevokedAt, DateTime.UtcNow));
    }

    public async Task<string> GetUserIdAsync(string name)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        return await context.Users.Where(user => user.UserName == name).Select(user => user.Id).SingleAsync();
    }

    public async Task<string> GetRoleIdAsync(string name)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        return await context.Roles.Where(role => role.Name == name).Select(role => role.Id).SingleAsync();
    }

    public async Task<string> CreateRoleAsync(string name)
    {
        await using var scope = Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var role = new IdentityRole(name);
        if (!(await roles.CreateAsync(role)).Succeeded)
            throw new InvalidOperationException("Could not create test role.");
        return role.Id;
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        await using var scope = Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (!await roles.RoleExistsAsync(roleName) && !(await roles.CreateAsync(new IdentityRole(roleName))).Succeeded)
            throw new InvalidOperationException("Could not seed test role.");
        var user = await users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Missing test user.");
        if (!(await users.AddToRoleAsync(user, roleName)).Succeeded)
            throw new InvalidOperationException("Could not assign test role.");
    }

    public async Task<string> CreateProofAsync(string userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(userId)
            ?? throw new InvalidOperationException("Missing test user.");
        var proof = await scope.ServiceProvider.GetRequiredService<IAccountSecurityProofService>().CreateAsync(user);
        return proof.IsSuccess ? proof.Value : throw new InvalidOperationException("Could not issue test proof.");
    }

    public async Task<int> CountUsersInRoleAsync(string roleName)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        return await (from assignment in context.UserRoles
                      join role in context.Roles on assignment.RoleId equals role.Id
                      where role.Name == roleName
                      select assignment.UserId).CountAsync();
    }

    public async Task AddPasswordUserAsync(string name, string? directPermission = null, string? rolePermission = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var user = new ApplicationUser { UserName = name, Email = $"{name}@example.test", EmailConfirmed = true };
        if (!(await users.CreateAsync(user, "P@ssword123!")).Succeeded)
            throw new InvalidOperationException("Could not create password test user.");
        if (directPermission is not null && !(await users.AddClaimAsync(user, new Claim("human_permission", directPermission))).Succeeded)
            throw new InvalidOperationException("Could not add direct test claim.");
        if (rolePermission is not null)
        {
            var roleName = $"Role-{name}";
            var role = new IdentityRole(roleName);
            if (!(await roles.CreateAsync(role)).Succeeded ||
                !(await roles.AddClaimAsync(role, new Claim("human_permission", rolePermission))).Succeeded ||
                !(await users.AddToRoleAsync(user, roleName)).Succeeded)
                throw new InvalidOperationException("Could not add role test claim.");
        }
    }

    public async Task<string> LoginAsync(string name) => (await LoginCredentialsAsync(name)).AccessToken;

    public async Task<AuthenticationResponseVM> LoginCredentialsAsync(string name)
    {
        await using var scope = Services.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        accessor.HttpContext.Request.Scheme = "https";
        try
        {
            var result = await scope.ServiceProvider.GetRequiredService<IAuthenticationService>()
                .LoginAsync(name, "P@ssword123!");
            return result.IsSuccess ? result.Value : throw new InvalidOperationException("Test login failed: " + string.Join(", ", result.Errors.Select(error => error.Message)));
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    public async Task<Guid> GetCurrentSessionIdAsync(string userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        return await context.RefreshSessions.Where(session => session.UserId == userId && !session.IsRevoked)
            .Select(session => session.PublicId).SingleAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_created)
        {
            if (!_databaseName.StartsWith("AhisIdentityPolicy_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to delete an unrecognized test database.");
            await using var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>()
                .UseSqlServer(ConnectionString).Options);
            await context.Database.EnsureDeletedAsync();
        }
    }

    private sealed class PolicyApiKeyValidator : IApiKeyValidator
    {
        public Task<ApiKeyValidationResult> ValidateAsync(string rawApiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(rawApiKey == "country-read" || rawApiKey == "country-write"
                ? ApiKeyValidationResult.Success(Guid.NewGuid(), Guid.NewGuid(), "test-client", "Test client", 10000,
                    rawApiKey == "country-write" ? ["country.write"] : ["country.read"], null)
                : ApiKeyValidationResult.Failure("invalid"));
        public Task UpdateLastUsedAsync(Guid apiClientKeyId, DateTime? observedLastUsedAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
    }

    private sealed class PolicyMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object result = request switch
            {
                GetHumanPermissionCatalogQuery => Result.Ok(new AdminPermissionCatalogVM([])),
                GetRolesQuery => Result.Ok(new PagedResult<List<AdminRoleListItemVM>>([], 0, 1, 20)),
                GetUserRolesQuery => Result.Ok(new List<AdminUserRoleVM>()),
                GetAllCountryQuery => Result.Ok(new List<CountryVM>()),
                GetCountryByIdQuery => Result.Ok(new CountryVM { CountryId = 1, CountryFullname = "Test", CountryShortname = "Test", CountryCode2 = "TT", CountryCode3 = "TST", CountryIsoCode = "000", CountryDescription = "Test" }),
                DeleteCountryCommand => Result.Ok(true),
                _ => throw new InvalidOperationException($"The policy test did not configure {request.GetType().Name}.")
            };
            return Task.FromResult((TResponse)result);
        }
    }
}
