using System.Security.Cryptography;
using System.Text;

namespace ahis.template.identity.Services;

public static class RefreshTokenHashing
{
    public static byte[] Compute(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        return SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
    }
}
