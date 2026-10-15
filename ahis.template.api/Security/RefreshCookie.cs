using Microsoft.AspNetCore.Http;

namespace ahis.template.api.Security;

internal static class RefreshCookie
{
    private const string Name = "refresh_token";
    private const string CanonicalPath = "/api";

    public static void Append(HttpResponse response, string value, DateTime expiresAt) =>
        response.Cookies.Append(Name, value, CreateOptions(CanonicalPath, expiresAt));

    public static void Clear(HttpResponse response)
    {
        var expiresAt = DateTimeOffset.UnixEpoch;
        response.Cookies.Append(Name, string.Empty, CreateOptions(CanonicalPath, expiresAt));

        // Remove cookies emitted by earlier API versions during the transition.
        response.Cookies.Append(Name, string.Empty, CreateOptions("/", expiresAt));
        response.Cookies.Append(Name, string.Empty, CreateOptions("/api/authentication/refresh", expiresAt));
    }

    private static CookieOptions CreateOptions(string path, DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = expiresAt,
        Path = path
    };
}
