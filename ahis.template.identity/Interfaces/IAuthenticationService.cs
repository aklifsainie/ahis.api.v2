using ahis.template.domain.Enums;
using FluentResults;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using ahis.template.identity.Models.DTOs;

namespace ahis.template.identity.Interfaces
{
    public interface IAuthenticationService
    {
        Task<Result<AuthenticationResponseVM>> LoginAsync(string userNameOrEmail, string password, bool rememberMe = false);
        //Task<Result> LogoutAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<Result<AuthenticationResponseVM>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<Result<AuthenticationResponseVM>> VerifyTwoFactorAsync(TwoFactorProviderEnum provider, string code, bool rememberMachine = false);
        Task<Result<AuthenticationResponseVM>> IssuePasskeySessionAsync(string userId, CancellationToken cancellationToken);

        Task<Result> RevokeRefreshTokensAsync(string userId);
        Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl);

        Task<Result> ResetPasswordAsync(string userId, string token, string newPassword, CancellationToken cancellationToken = default);
        Task<Result> StartAccountRecoveryAsync(string email, CancellationToken cancellationToken);
        Task<Result> CompleteAccountRecoveryAsync(
            string challenge,
            string newPassword,
            TwoFactorProviderEnum? twoFactorProvider,
            string? twoFactorCode,
            CancellationToken cancellationToken);

    }

}
