using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


namespace ahis.template.application.Features.AccountFeatures.Commands
{
    /// <summary>
    /// Verify authenticator code and enable 2FA for the user
    /// </summary>
    public class EnableTwoFactorCommand : IRequest<Result<IEnumerable<string>>>
    {

        /// <summary>
        /// 6-digit authenticator verification code
        /// </summary>
        public string VerificationCode { get; set; } = default!;
    }

    public class VerifyTwoFactorCommandHandler: IRequestHandler<EnableTwoFactorCommand, Result<IEnumerable<string>>>
    {
        private readonly IAccountService _accountService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<VerifyTwoFactorCommandHandler> _logger;

        public VerifyTwoFactorCommandHandler(
            IAccountService accountService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<VerifyTwoFactorCommandHandler> logger)
        {
            _accountService = accountService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<string>>> Handle(
            EnableTwoFactorCommand request,
            CancellationToken cancellationToken)
        {

            var userId = _httpContextAccessor.HttpContext?
                .User?
                .FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Result.Fail<IEnumerable<string>>("UserId is required.");

            if (string.IsNullOrWhiteSpace(request.VerificationCode))
                return Result.Fail<IEnumerable<string>>("Verification code is required.");

            try
            {
                return await _accountService.EnableAuthenticatorAsync(
                    userId,
                    request.VerificationCode
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error enabling authenticator for user {UserId}",
                    userId);

                return Result.Fail<IEnumerable<string>>(
                    "An unexpected error occurred while enabling authenticator.");
            }
        }
    }
}
