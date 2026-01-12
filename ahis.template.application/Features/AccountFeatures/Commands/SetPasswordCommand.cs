using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.application.Features.AccountFeatures.Commands
{
    public class SetPasswordCommand : IRequest<Result>
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }


    public class SetPasswordCommandHandler : IRequestHandler<SetPasswordCommand, Result>
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<SetPasswordCommandHandler> _logger;

        public SetPasswordCommandHandler(IAccountService accountService, ILogger<SetPasswordCommandHandler> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        public async Task<Result> Handle(SetPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Setting password for user {UserId}",
                request.UserId
            );

            var result = await _accountService
                .SetPasswordFirstTimeAsync(request.UserId, request.Password);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to set password for user {UserId}: {Error}",
                    request.UserId,
                    result.Errors.FirstOrDefault()?.Message
                );

                return Result.Fail(
                    result.Errors.FirstOrDefault()?.Message
                    ?? "Failed to set password."
                );
            }

            return Result.Ok();
        }
    }
}
