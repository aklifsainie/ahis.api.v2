using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Mediator;
using ahis.template.application.Interfaces.Commons;
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
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<VerifyTwoFactorLoginCommandHandler> _logger;

        public VerifyTwoFactorLoginCommandHandler(
            IAuthenticationService authenticationService,
            IAuditLogger auditLogger,
            ILogger<VerifyTwoFactorLoginCommandHandler> logger)
        {
            _authenticationService = authenticationService;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Result<AuthenticationResponseVM>> Handle(VerifyTwoFactorLoginCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Result.Fail("Verification code is required.");
            }
                

            try
            {
                var result = await _authenticationService.VerifyTwoFactorAsync(request.Provider, request.Code, request.RememberMachine);
                await _auditLogger.LogAsync(
                    result.IsSuccess ? AuditActionEnum.Login : AuditActionEnum.LoginFailed,
                    "Authentication",
                    result.IsSuccess ? result.Value.UserId : "pre-auth",
                    result.IsSuccess ? "TwoFactorAuthentication" : "TwoFactorAuthenticationFailed",
                    overrideUserId: result.IsSuccess ? result.Value.UserId : null,
                    cancellationToken: cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "2FA verification failed.");

                return Result.Fail("Failed to verify two-factor authentication.");
            }
        }
    }
}
