using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.application.Features.AccountFeatures.Commands
{
    public class RegisterUserCommand : IRequest<Result>
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string UserName { get; set; } = null!;

        // Client should provide a base url for email callback (frontend)
        [Required]
        public string CallbackBaseUrl { get; set; } = null!;
    }


    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(IAccountService accountService, ILogger<RegisterUserCommandHandler> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering user {Email}", request.Email);
            var result = await _accountService.RegisterAsync(request.Email, request.UserName, request.CallbackBaseUrl);
            if (!result.IsSuccess)
            {
                _logger.LogWarning($"Failed to register user {request.Email}: {result.Errors.FirstOrDefault()?.Message}");

                return Result.Fail(result.Errors.FirstOrDefault()?.Message ?? "Registration failed.");
            }

            return Result.Ok();
        }
    }
}
