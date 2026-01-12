using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.AuthenticationFeatures.Commands
{
    public record LoginCommand(string UserNameOrEmail, string Password, bool RememberMe) : IRequest<Result<AuthenticationResponseVM>>;

    public class LoginCommandHandler: IRequestHandler<LoginCommand, Result<AuthenticationResponseVM>>
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(IAuthenticationService authenticationService, ILogger<LoginCommandHandler> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        public async Task<Result<AuthenticationResponseVM>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var result = await _authenticationService.LoginAsync(
                request.UserNameOrEmail,
                request.Password,
                request.RememberMe);

            if (result.IsFailed)
            {
                _logger.LogWarning(
                    "Login failed for user {UserNameOrEmail}: {Errors}",
                    request.UserNameOrEmail,
                    string.Join(", ", result.Errors.Select(e => e.Message)));

                return Result.Fail<AuthenticationResponseVM>(result.Errors);
            }

            return Result.Ok(result.Value);
        }
    }
}
