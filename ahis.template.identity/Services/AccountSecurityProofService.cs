using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ahis.template.identity.Services;

public sealed class AccountSecurityProofService : IAccountSecurityProofService
{
    private const string PurposeClaim = "account_security_proof";
    private const string Purpose = "account-security";
    private readonly IConfiguration _configuration;
    private readonly IIdentityTokenStateService _tokenState;

    public AccountSecurityProofService(
        IConfiguration configuration,
        IIdentityTokenStateService tokenState)
    {
        _configuration = configuration;
        _tokenState = tokenState;
    }

    public Task<Result<string>> CreateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var securityVersion = _tokenState.GetSecurityVersion(user);
        var signingKey = _configuration["Jwt:SigningKey"];
        if (securityVersion is null || string.IsNullOrWhiteSpace(signingKey))
            return Task.FromResult(Result.Fail<string>("Unable to create security proof."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(IIdentityTokenStateService.SecurityVersionClaim, securityVersion),
            new Claim(IIdentityTokenStateService.TokenUseClaim, "step-up"),
            new Claim(PurposeClaim, Purpose),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Task.FromResult(Result.Ok(new JwtSecurityTokenHandler().WriteToken(token)));
    }

    public async Task<bool> IsValidAsync(string userId, string? proof, CancellationToken cancellationToken = default)
    {
        var signingKey = _configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(proof) || string.IsNullOrWhiteSpace(signingKey))
            return false;

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(proof, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.Zero
            }, out _);

            var proofUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var version = principal.FindFirstValue(IIdentityTokenStateService.SecurityVersionClaim);
            var purpose = principal.FindFirstValue(PurposeClaim);
            return proofUserId == userId && purpose == Purpose &&
                await _tokenState.ValidateAsync(userId, version, cancellationToken);
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
