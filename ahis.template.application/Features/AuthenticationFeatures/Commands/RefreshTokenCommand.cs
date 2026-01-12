using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using ahis.template.identity.Interfaces;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AuthenticationFeatures.Commands
{
    public sealed class RefreshTokenCommand : IRequest<Result<AuthenticationResponseVM>>
    {
        public string RefreshToken { get; }

        public RefreshTokenCommand(string refreshToken)
        {
            RefreshToken = refreshToken;
        }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthenticationResponseVM>>
    {

        private readonly IAuthenticationService _authenticationService;

        public RefreshTokenCommandHandler(
            IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<Result<AuthenticationResponseVM>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            return await _authenticationService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        }
    }
}
