using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Features.AccountFeatures.Queries;
using ahis.template.application.Features.IdentityRoleManagementFeatures;
using ahis.template.api.Security;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AccountVM;
using ahis.template.domain.Models.ViewModels.IdentityRoleVM;
using FluentResults;
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
                return Problem(
                    title: "Resource not found",
                    detail: "Resource not found.",
                    statusCode: StatusCodes.Status404NotFound);

            return ValidationProblem();
        }

        var actorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (actorUserId == userId)
            RefreshCookie.Clear(Response);

        return NoContent();
    }

    /// <summary>Clears an active ordinary lockout for an Identity user.</summary>
    /// <remarks>
    /// Requires the Superadmin role and a current actor-bound five-minute step-up proof in
    /// <c>X-Step-Up-Proof</c>. This action cannot unlock the caller and does not disclose the
    /// target's restriction state.
    /// </remarks>
    [HttpPost("{userId}/unlock")]
    [Authorize(Policy = "IdentityAdminUserUnlockPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Unlock(string userId, [FromHeader(Name = "X-Step-Up-Proof")] string? stepUpProof)
    {
        var result = await _mediator.Send(
            new UnlockAdminUserCommand { UserId = userId, StepUpProof = stepUpProof },
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

        return NoContent();
    }

    /// <summary>Places an administrative security hold on an eligible account.</summary>
    [HttpPost("{userId}/disable")]
    [Authorize(Policy = "IdentityAdminUserAccessPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Disable(string userId, [FromHeader(Name = "X-Step-Up-Proof")] string? stepUpProof) =>
        ChangeAdminUserAccess(userId, stepUpProof, disable: true);

    /// <summary>Closes an active administrative security hold without changing other account state.</summary>
    [HttpPost("{userId}/enable")]
    [Authorize(Policy = "IdentityAdminUserAccessPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Enable(string userId, [FromHeader(Name = "X-Step-Up-Proof")] string? stepUpProof) =>
        ChangeAdminUserAccess(userId, stepUpProof, disable: false);

    /// <summary>Resets an eligible user's authenticator and recovery codes.</summary>
    [HttpPost("{userId}/reset-mfa")]
    [Authorize(Policy = "IdentityAdminUserAccessPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> ResetMfa(string userId, [FromHeader(Name = "X-Step-Up-Proof")] string? stepUpProof) =>
        ChangeAdminCredential(userId, stepUpProof, resetMfa: true);

    /// <summary>Requires an eligible user to complete the password reset flow before signing in.</summary>
    [HttpPost("{userId}/require-password-reset")]
    [Authorize(Policy = "IdentityAdminUserAccessPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RequirePasswordReset(string userId, [FromHeader(Name = "X-Step-Up-Proof")] string? stepUpProof) =>
        ChangeAdminCredential(userId, stepUpProof, resetMfa: false);

    private async Task<IActionResult> ChangeAdminCredential(string userId, string? proof, bool resetMfa)
    {
        var result = resetMfa
            ? await _mediator.Send(new ResetAdminUserMfaCommand { UserId = userId, StepUpProof = proof }, HttpContext.RequestAborted)
            : await _mediator.Send(new RequireAdminUserPasswordResetCommand { UserId = userId, StepUpProof = proof }, HttpContext.RequestAborted);
        Response.Headers.CacheControl = "no-store";
        if (result.IsFailed)
        {
            if (result.Errors.Any(error => error.Metadata.ContainsKey("OperationalFailure"))) return Problem(statusCode: StatusCodes.Status500InternalServerError);
            if (result.Errors.Any(error => error is ahis.template.application.Shared.Errors.EntityNotFoundError))
                return Problem(title: "Resource not found", detail: "Resource not found.", statusCode: StatusCodes.Status404NotFound);
            return ValidationProblem();
        }
        return NoContent();
    }

    private async Task<IActionResult> ChangeAdminUserAccess(string userId, string? proof, bool disable)
    {
        var result = disable
            ? await _mediator.Send(new DisableAdminUserCommand { UserId = userId, StepUpProof = proof }, HttpContext.RequestAborted)
            : await _mediator.Send(new EnableAdminUserCommand { UserId = userId, StepUpProof = proof }, HttpContext.RequestAborted);
        Response.Headers.CacheControl = "no-store";
        if (result.IsFailed)
        {
            if (result.Errors.Any(error => error.Metadata.ContainsKey("OperationalFailure"))) return Problem(statusCode: StatusCodes.Status500InternalServerError);
            if (result.Errors.Any(error => error is ahis.template.application.Shared.Errors.EntityNotFoundError))
                return Problem(title: "Resource not found", detail: "Resource not found.", statusCode: StatusCodes.Status404NotFound);
            return ValidationProblem();
        }
        return NoContent();
    }

    /// <summary>Gets direct Identity-role assignments for a user.</summary>
    [HttpGet("{userId}/roles")]
    [Authorize(Policy = "IdentityRoleReadPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(typeof(ResponseDto<List<AdminUserRoleVM>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(string userId)
    {
        var result = await _mediator.Send(new GetUserRolesQuery { UserId = userId }, HttpContext.RequestAborted);
        Response.Headers.CacheControl = "no-store";
        return result.IsSuccess ? Ok(new ResponseDto<List<AdminUserRoleVM>>(result.Value)) : RoleError(result.Errors);
    }

    /// <summary>Replaces a user's complete direct Identity-role assignment set.</summary>
    [HttpPut("{userId}/roles")]
    [Authorize(Policy = "IdentityRoleAssignmentPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReplaceRoles(string userId, [FromBody] RoleIdsBody body,
        [FromHeader(Name = "X-Step-Up-Proof")] string? stepUpProof)
    {
        var result = await _mediator.Send(new ReplaceUserRolesCommand
        { UserId = userId, RoleIds = body?.RoleIds, StepUpProof = stepUpProof }, HttpContext.RequestAborted);
        Response.Headers.CacheControl = "no-store";
        return result.IsSuccess ? NoContent() : RoleError(result);
    }

    private IActionResult RoleError(Result result) => RoleError(result.Errors);
    private IActionResult RoleError(IEnumerable<IError> errors)
    {
        var error = errors.First();
        if (error.Metadata.ContainsKey("NotFound")) return Problem(statusCode: StatusCodes.Status404NotFound);
        if (error.Metadata.ContainsKey("Conflict")) return Problem(statusCode: StatusCodes.Status409Conflict);
        if (error.Metadata.ContainsKey("Validation")) return ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        return Problem(statusCode: StatusCodes.Status500InternalServerError);
    }

    public sealed class RoleIdsBody { public IReadOnlyCollection<string>? RoleIds { get; init; } }
}
