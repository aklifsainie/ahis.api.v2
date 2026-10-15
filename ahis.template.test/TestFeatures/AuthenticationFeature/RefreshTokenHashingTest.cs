using ahis.template.identity.Services;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace ahis.template.test.TestFeatures.AuthenticationFeature;

public class RefreshTokenHashingTest
{
    [Fact]
    public void ComputeUsesSha256AndDoesNotReturnTheRawToken()
    {
        const string refreshToken = "test-refresh-token";

        var hash = RefreshTokenHashing.Compute(refreshToken);

        hash.Should().BeEquivalentTo(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        Encoding.UTF8.GetString(hash).Should().NotBe(refreshToken);
    }

    [Fact]
    public void ComputeRejectsMissingToken()
    {
        var action = () => RefreshTokenHashing.Compute(" ");

        action.Should().Throw<ArgumentException>();
    }
}
