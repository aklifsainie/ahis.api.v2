using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Features.AccountFeatures.Queries;
using ahis.template.api.Security;
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

    /// <summary>Revokes all access and refresh sessions for an Identity user.</summary>
    /// <remarks>
    /// Requires the Superadmin role and a current actor-bound five-minute step-up proof in
    /// <c>X-Step-Up-Proof</c>. This action returns no target data, sends no notification, and
    /// clears the caller's refresh cookie only when the caller revokes their own sessions.
    /// </remarks>
    [HttpPost("{userId}/revoke-sessions")]
    [Authorize(Policy = "IdentityAdminSessionRevocationPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevokeSessions(
        string userId,
        [FromHeader(Name = "X-Step-Up-Proof")] string? stepUpProof)
    {
        var result = await _mediator.Send(
            new RevokeAdminUserSessionsCommand { UserId = userId, StepUpProof = stepUpProof },
            HttpContext.RequestAborted);
        Response.Headers.CacheControl = "no-store";
        if (result.IsFailed)
        {
            if (result.Errors.Any(error => error.Metadata.ContainsKey("OperationalFailure")))
                return Problem(statusCode: StatusCodes.Status500InternalServerError);

            if (result.Errors.Any(error => error is ahis.template.application.Shared.Errors.EntityNotFoundError))
                return ToActionResult(result);

            return ValidationProblem();
        }

        var actorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (actorUserId == userId)
            RefreshCookie.Clear(Response);

        return NoContent();
    }
}
