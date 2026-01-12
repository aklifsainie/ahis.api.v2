using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AccountFeatures.Commands
{
    public class ChangePasswordCommand: IRequest<Result>
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }


        public ChangePasswordCommand(string currentPassword, string newPassword)
        {
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
        }


    }

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
    {
        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUserService;

        public ChangePasswordCommandHandler(IAccountService accountService, ICurrentUserService currentUserService)
        {
            _accountService = accountService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Fail("User is not authenticated.");
            }
                

            return await _accountService.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword,
                cancellationToken);
        }
    }


}
