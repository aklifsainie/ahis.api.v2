using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


namespace ahis.template.application.Features.AccountFeatures.Commands
{
    public class DisableTwoFactorCommand: IRequest<Result>
    {
    }

    public class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, Result>
    {
        private readonly IAccountService _accountService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DisableTwoFactorCommandHandler> _logger;

        public DisableTwoFactorCommandHandler(IAccountService accountService, IHttpContextAccessor httpContextAccessor, ILogger<DisableTwoFactorCommandHandler> logger)
        {
            _accountService = accountService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?
                .User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Fail("Unauthorized");
            }

            var result = await _accountService.DisableAuthenticatorAsync(userId);

            if (result.IsFailed)
            {
                _logger.LogWarning("Failed to disable authenticator");
                return Result.Fail(result.Errors);
            }

            return Result.Ok();
        }
    }
}
