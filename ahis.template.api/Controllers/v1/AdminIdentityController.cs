using ahis.template.application.Features.AccountFeatures.Queries;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AccountVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ahis.template.api.Controllers.v1;

[Route("api/admin/identity/users")]
[ApiController]
public sealed class AdminIdentityController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminIdentityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Gets a limited security-state projection for an Identity user.</summary>
    /// <remarks>
    /// Requires the Superadmin role. The response excludes credentials, tokens, authenticator material,
    /// recovery-code values, security stamps, and audit values.
    /// </remarks>
    [HttpGet("{userId}/security-state")]
    [Authorize(Policy = "IdentityAdminSecurityReadPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(typeof(ResponseDto<AdminUserSecurityStateResponseVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetSecurityState(string userId)
    {
        var result = await _mediator.Send(
            new GetAdminUserSecurityStateQuery { UserId = userId },
            HttpContext.RequestAborted);

        Response.Headers.CacheControl = "no-store";
        return ToActionResult(result);
    }
}
