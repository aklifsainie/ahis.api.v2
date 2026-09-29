using ahis.template.application.Features.IdentityRoleManagementFeatures;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.IdentityRoleVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ahis.template.api.Controllers.v1;

[ApiController]
[Route("api/admin/identity/permissions")]
public sealed class AdminIdentityPermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminIdentityPermissionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Gets the server-owned catalog of assignable human role permissions.
    /// </summary>
    /// <remarks>
    /// Requires a bearer-authenticated Superadmin. The catalog is for administrative role editing;
    /// the server remains authoritative for authorization and permission validation.
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = "IdentityRoleReadPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(typeof(ResponseDto<AdminPermissionCatalogVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [Produces("application/json")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHumanPermissionCatalogQuery(), cancellationToken);
        Response.Headers.CacheControl = "no-store";

        return result.IsSuccess
            ? Ok(new ResponseDto<AdminPermissionCatalogVM>(result.Value))
            : Problem(statusCode: StatusCodes.Status500InternalServerError);
    }
}
