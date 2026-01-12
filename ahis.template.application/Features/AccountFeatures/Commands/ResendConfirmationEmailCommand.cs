using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using FluentResults;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AccountFeatures.Commands
{
    public class ResendConfirmationEmailCommand : IRequest<Result>
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string CallbackBaseUrl { get; set; }
    }


    public class ResendConfirmationEmailCommandHandler : IRequestHandler<ResendConfirmationEmailCommand, Result>
    {
        private readonly IAccountService _accountService;

        public ResendConfirmationEmailCommandHandler(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<Result> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
        {
            var result = await _accountService.ResendConfirmationEmailAsync(request.Email, request.CallbackBaseUrl, cancellationToken);

            if (result.IsFailed)
            {
                Result.Fail(result.Errors);
            }

            return result;
        }
    }
}
