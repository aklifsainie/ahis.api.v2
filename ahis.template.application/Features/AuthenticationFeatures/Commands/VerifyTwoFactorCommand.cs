using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.AuthenticationFeatures.Commands
{
    /// <summary>
    /// Verify two-factor authentication code and complete login
    /// </summary>
    public class VerifyTwoFactorLoginCommand : IRequest<Result<AuthenticationResponseVM>>
    {
        public string UserId { get; set; } = default!;

        /// <summary>
        /// Two-factor provider (Authenticator or RecoveryCode)
        /// </summary>
        public TwoFactorProviderEnum Provider { get; set; }

        /// <summary>
        /// Verification code or recovery code
        /// </summary>
        public string Code { get; set; } = default!;

        /// <summary>
        /// Remember this device (Authenticator only)
        /// </summary>
        public bool RememberMachine { get; set; } = false;
    }


    public class VerifyTwoFactorLoginCommandHandler: IRequestHandler<VerifyTwoFactorLoginCommand, Result<AuthenticationResponseVM>>
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<VerifyTwoFactorLoginCommandHandler> _logger;

        public VerifyTwoFactorLoginCommandHandler(IAuthenticationService authenticationService, ICurrentUserService currentUserService, ILogger<VerifyTwoFactorLoginCommandHandler> logger)
        {
            _authenticationService = authenticationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<AuthenticationResponseVM>> Handle(VerifyTwoFactorLoginCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Fail("User is not authenticated.");
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Result.Fail("Verification code is required.");
            }
                

            try
            {
                var result = await _authenticationService.VerifyTwoFactorAsync(userId, request.Provider, request.Code, request.RememberMachine);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "2FA verification failed for user {UserId}",
                    request.UserId);

                return Result.Fail("Failed to verify two-factor authentication.");
            }
        }
    }
}
