using ahis.template.application.Features.ApiKeyAuthenticationFeatures.Commands;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using ahis.template.identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ahis.template.api.Controllers.v1
{
    [Route("api/apiclient")]
    [ApiController]
    [Authorize(Policy = "ApiClientAdminPolicy")]
    [EnableRateLimiting("AuthenticatedSecurityPolicy")]
    public class ApiClientController : ControllerBase
    {
        private readonly IApiClientService _apiClientService;
        private readonly IIdentityApiClientAdministrationService _administration;

        public ApiClientController(IApiClientService apiClientService, IIdentityApiClientAdministrationService administration)
        {
            _apiClientService = apiClientService;
            _administration = administration;
        }

        /// <summary>
        /// Creates an external API client and its first API key.
        /// The raw API key is returned only once.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CreateApiClientResponseVM), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateClient([FromBody] CreateApiClientCommand command, [FromHeader(Name = "X-Step-Up-Proof")] string? proof, CancellationToken cancellationToken)
        {
            var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var authorization = await AuthorizeAsync(createdBy, proof, cancellationToken);
            if (authorization is not null) return authorization;
            var actorUserId = createdBy!;

            try
            {
                var response = await _apiClientService.CreateClientAsync(command, actorUserId, cancellationToken);

                return CreatedAtAction(nameof(CreateClient), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Creates an additional key for key rotation.
        /// </summary>
        [HttpPost("{apiClientId:guid}/keys")]
        [ProducesResponseType(typeof(CreateApiClientKeyResponseVM), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateKey(Guid apiClientId, [FromBody] CreateApiClientKeyCommand command, [FromHeader(Name = "X-Step-Up-Proof")] string? proof, CancellationToken cancellationToken)
        {
            var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var authorization = await AuthorizeAsync(createdBy, proof, cancellationToken);
            if (authorization is not null) return authorization;
            var actorUserId = createdBy!;

            try
            {
                var response = await _apiClientService.CreateKeyAsync(apiClientId, command, actorUserId, cancellationToken);

                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{apiClientId:guid}/keys/{keyId:guid}/revoke")]
        public async Task<IActionResult> RevokeKey(Guid apiClientId, Guid keyId, [FromBody] RevokeApiClientKeyRequest request, [FromHeader(Name = "X-Step-Up-Proof")] string? proof, CancellationToken cancellationToken)
        {
            var revokedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var authorization = await AuthorizeAsync(revokedBy, proof, cancellationToken);
            if (authorization is not null) return authorization;
            var actorUserId = revokedBy!;

            try
            {
                await _apiClientService.RevokeKeyAsync(apiClientId, keyId, request.Reason, actorUserId, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{apiClientId:guid}/deactivate")]
        public async Task<IActionResult> DeactivateClient(Guid apiClientId, [FromBody] DeactivateApiClientRequest request, [FromHeader(Name = "X-Step-Up-Proof")] string? proof, CancellationToken cancellationToken)
        {
            var deactivatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var authorization = await AuthorizeAsync(deactivatedBy, proof, cancellationToken);
            if (authorization is not null) return authorization;
            var actorUserId = deactivatedBy!;

            try
            {
                await _apiClientService.DeactivateClientAsync(apiClientId, request.Reason, actorUserId, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private async Task<IActionResult?> AuthorizeAsync(string? actorUserId, string? proof, CancellationToken cancellationToken)
        {
            if (Request.Headers["X-Step-Up-Proof"].Count > 1)
                return BadRequest(new ProblemDetails { Title = "Invalid step-up proof.", Status = StatusCodes.Status400BadRequest });

            var result = await _administration.ValidateAsync(actorUserId ?? string.Empty, proof, cancellationToken);
            if (result.IsSuccess) return null;

            return result.Errors.Any(error => error.Metadata.ContainsKey("Forbidden"))
                ? Forbid()
                : BadRequest(new ProblemDetails { Title = "Unable to authorize API-client administration.", Status = StatusCodes.Status400BadRequest });
        }
    }

    public sealed class RevokeApiClientKeyRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class DeactivateApiClientRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
