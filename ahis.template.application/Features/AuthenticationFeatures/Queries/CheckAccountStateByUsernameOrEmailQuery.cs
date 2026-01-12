using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using ahis.template.identity.Interfaces;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AuthenticationFeatures.Queries
{
    public class CheckAccountStateByUsernameOrEmailQuery : IRequest<Result<CheckAccountStateVM>>
    {
        public string userNameOrEmail { get; set; }
    }

    public class CheckAccountStateByEmailQueryHandler : IRequestHandler<CheckAccountStateByUsernameOrEmailQuery, Result<CheckAccountStateVM>>
    {
        private readonly IAuthenticationService _authenticationService;

        public CheckAccountStateByEmailQueryHandler(
            IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<Result<CheckAccountStateVM>> Handle(CheckAccountStateByUsernameOrEmailQuery request, CancellationToken cancellationToken)
        {

            var result = await _authenticationService.CheckAccountStateByEmailAsync(request.userNameOrEmail);
            var dto = new CheckAccountStateVM
            { 
                RequiresTwoFactor = result.Value.RequiresTwoFactor,
                IsEmailConfirmed = result.Value.IsEmailConfirmed,
                IsPasswordCreated = result.Value.IsPasswordCreated

            };

            return Result.Ok<CheckAccountStateVM>(dto).WithSuccess("Account state obtained");
        }
    }
}
