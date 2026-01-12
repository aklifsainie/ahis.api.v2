using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.application.Features.AccountFeatures.Commands
{
    public class ConfirmEmailCommand : IRequest<Result>
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!; // base64url encoded
    }

    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<ConfirmEmailCommandHandler> _logger;

        public ConfirmEmailCommandHandler(IAccountService accountService, ILogger<ConfirmEmailCommandHandler> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Confirming email for user {UserId}", request.UserId);
            var result = await _accountService.ConfirmEmailAsync(request.UserId, request.Token);
            if (!result.IsSuccess)
            {
                _logger.LogWarning($"Failed to register user {request.UserId}: {result.Errors.FirstOrDefault()?.Message}");

                return Result.Fail(result.Errors.FirstOrDefault()?.Message ?? "Confirm email failed.");
            }
                

            return Result.Ok();
        }
    }
}
