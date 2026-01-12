using ahis.template.domain.Enums;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using FluentResults;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ahis.template.identity.Interfaces
{
    public interface IAuthenticationService
    {
        Task<Result<AuthenticationResponseVM>> CheckAccountStateByEmailAsync(string email);
        Task<Result<AuthenticationResponseVM>> LoginAsync(string userNameOrEmail, string password, bool rememberMe = false);
        //Task<Result> LogoutAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<Result<AuthenticationResponseVM>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<Result<AuthenticationResponseVM>> VerifyTwoFactorAsync(string userId, TwoFactorProviderEnum provider, string code, bool rememberMachine = false);
        Task<Result> RevokeRefreshTokensAsync(string userId);
        Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl);

        Task<Result> ResetPasswordAsync(string userId, string token, string newPassword);

        JwtPayload DecodeToken(string token);
        IEnumerable<Claim> GetClaims(string token);
    }

}
