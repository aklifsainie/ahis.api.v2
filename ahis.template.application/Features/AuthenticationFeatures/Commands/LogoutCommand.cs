using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AuthenticationFeatures.Commands
{
    public record LogoutCommand(string? RefreshToken) : IRequest<Result>;


    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IAuthenticationService _authenticationService;

        public LogoutCommandHandler(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // Logout must be idempotent
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Result.Ok();

            await _authenticationService.LogoutAsync(request.RefreshToken);

            return Result.Ok();
        }
    }
}
