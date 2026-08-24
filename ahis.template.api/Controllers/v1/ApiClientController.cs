using ahis.template.application.Features.ApiKeyAuthenticationFeatures.Commands;
using ahis.template.application.Interfaces.Services;
using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ahis.template.api.Controllers.v1
{
    [Route("api/apiclient")]
    [ApiController]
    [EnableRateLimiting("ApiPolicy")]
    public class ApiClientController : ControllerBase
    {
        private readonly IApiClientService _apiClientService;

        public ApiClientController(IApiClientService apiClientService)
        {
            _apiClientService = apiClientService;
        }

        /// <summary>
        /// Creates an external API client and its first API key.
        /// The raw API key is returned only once.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CreateApiClientResponseVM), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateClient([FromBody] CreateApiClientCommand command, CancellationToken cancellationToken)
        {
            var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var response = await _apiClientService.CreateClientAsync(command, createdBy, cancellationToken);

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
        public async Task<IActionResult> CreateKey(Guid apiClientId, [FromBody] CreateApiClientKeyCommand command, CancellationToken cancellationToken)
        {
            var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var response = await _apiClientService.CreateKeyAsync(apiClientId, command, createdBy, cancellationToken);

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
        public async Task<IActionResult> RevokeKey(Guid apiClientId, Guid keyId, [FromBody] RevokeApiClientKeyRequest request, CancellationToken cancellationToken)
        {
            var revokedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _apiClientService.RevokeKeyAsync(apiClientId, keyId, request.Reason, revokedBy, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{apiClientId:guid}/deactivate")]
        public async Task<IActionResult> DeactivateClient(Guid apiClientId, [FromBody] DeactivateApiClientRequest request, CancellationToken cancellationToken)
        {
            var deactivatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _apiClientService.DeactivateClientAsync(apiClientId, request.Reason, deactivatedBy, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
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
