using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using ahis.template.domain.Enums;
using FluentResults;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AuthenticationFeatures.Commands
{
    public class ResetPasswordCommand : IRequest<Result>
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public string NewPassword { get; set; }

        public TwoFactorProviderEnum? TwoFactorProvider { get; set; }

        public string? TwoFactorCode { get; set; }

    }

    public class ResetPasswordCommandHandler: IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IAuthenticationService _authenticationService;

        public ResetPasswordCommandHandler(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<Result> Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            var result = await _authenticationService.CompleteAccountRecoveryAsync(
                request.Token,
                request.NewPassword,
                request.TwoFactorProvider,
                request.TwoFactorCode,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Fail(result.Errors);
            }

            return Result.Ok();
        }
    }
}
