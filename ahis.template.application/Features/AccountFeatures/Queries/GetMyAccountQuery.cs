using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AccountVM;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AccountFeatures.Queries
{
    public class GetMyAccountQuery: IRequest<Result<GetMyAccountResponseVM>>
    {
    }

    public class GetMyAccountQueryHandler : IRequestHandler<GetMyAccountQuery, Result<GetMyAccountResponseVM>>
    {

        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUserService;
        public GetMyAccountQueryHandler(IAccountService accountService, ICurrentUserService currentUserService)
        {
            _accountService = accountService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetMyAccountResponseVM>> Handle(GetMyAccountQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUserService.IsAuthenticated)
                {
                    return Result.Fail("User is not authenticated.");
                }
                    

                var result = await _accountService.GetMyAccountAsync(_currentUserService.UserId!, cancellationToken);

                GetMyAccountResponseVM vm = new GetMyAccountResponseVM
                {
                    UserId = result.Value.UserId,
                    Email = result.Value.Email,
                    Username = result.Value.Username,
                    FirstName = result.Value.FirstName,
                    LastName = result.Value.LastName,
                    DateOfBirth = result.Value.DateOfBirth,
                    PhoneNumber = result.Value.PhoneNumber,
                    TwoFactorEnabled = result.Value.TwoFactorEnabled
                };

                return Result.Ok(vm).WithSuccess("User details obtained");
            }
            catch (Exception ex)
            {
                return Result.Fail<GetMyAccountResponseVM>(new Error($"An error occurred while retrieving account information: {ex.Message}"));
            }
        }
    }

}
