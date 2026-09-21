using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace ahis.template.test.TestFeatures.AccountFeature;

public class AccountSecurityProofServiceTest
{
    private const string SigningKey = "a-test-signing-key-that-is-long-enough-for-hmac-sha256";

    [Fact]
    public async Task CreatedProofIsBoundToTheAuthenticatedUserAndSecurityVersion()
    {
        var user = new ApplicationUser { Id = "user-1", SecurityStamp = "security-stamp" };
        var tokenState = new Mock<IIdentityTokenStateService>();
        tokenState.Setup(service => service.GetSecurityVersion(user)).Returns("version-1");
        tokenState.Setup(service => service.ValidateAsync(user.Id, "version-1")).ReturnsAsync(true);
        var service = CreateService(tokenState.Object);

        var proof = await service.CreateAsync(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(proof.Value);

        proof.IsSuccess.Should().BeTrue();
        token.Claims.Should().Contain(claim =>
            claim.Type == IIdentityTokenStateService.TokenUseClaim && claim.Value == "step-up");
        (await service.IsValidAsync(user.Id, proof.Value)).Should().BeTrue();
        (await service.IsValidAsync("other-user", proof.Value)).Should().BeFalse();
    }

    private static AccountSecurityProofService CreateService(IIdentityTokenStateService tokenState)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        return new AccountSecurityProofService(configuration, tokenState);
    }
}
