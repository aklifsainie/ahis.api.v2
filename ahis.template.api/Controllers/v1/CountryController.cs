using ahis.template.api.ApiClientAuthentication;
using ahis.template.application.Features.CountryFeatures.Command;
using ahis.template.application.Features.CountryFeatures.Query;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.CountryVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.api.Controllers.v1
{
    [Route("api/v1/country")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer," + ApiKeyAuthenticationDefault.AuthenticationScheme)]
    [EnableRateLimiting("ApiPolicy")]
    public class CountryController : BaseApiController
    {
        private readonly IMediator _mediator;

        public CountryController(IMediator mediator)
        {
            _mediator = mediator;
        }


        /// <summary>
        /// Get all countries
        /// </summary>
        /// <remarks>
        /// This endpoint returns a list of all active countries.
        /// </remarks>
        /// <param name="CountryFullname">Filter only active countries</param>
        /// <response code="200">Successfully retrieved the list of countries</response>
        /// <response code="500">Unexpected internal server error</response>
        /// <returns>This is a return messsage</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseDto<List<CountryVM>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Produces("application/json")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetAllCountryQuery();
            return ToActionResult(await _mediator.Send(query, cancellationToken));

        }

        /// <summary>
        /// Get an active country by ID.
        /// </summary>
        /// <param name="id">The unique ID of the country.</param>
        /// <param name="cancellationToken">
        /// Token used to cancel the request.
        /// </param>
        /// <response code="200">
        /// The country was retrieved successfully.
        /// </response>
        /// <response code="400">
        /// The supplied country ID is invalid.
        /// </response>
        /// <response code="401">
        /// Authentication is required.
        /// </response>
        /// <response code="403">
        /// The authenticated client is not authorized.
        /// </response>
        /// <response code="404">
        /// No active country exists with the supplied ID.
        /// </response>
        /// <response code="429">
        /// The request rate limit was exceeded.
        /// </response>
        /// <response code="500">
        /// An unexpected server error occurred.
        /// </response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ResponseDto<CountryVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> GetById([FromRoute, Range(1, int.MaxValue)] int id, CancellationToken cancellationToken)
        {
            var query = new GetCountryByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Add country
        /// </summary>
        /// <remarks>
        /// This endpoint handle add country
        /// </remarks>
        /// <param name="CountryFullname">The country's fullname</param>
        /// <response code="200">Successfully added the country</response>
        /// <response code="500">Unexpected internal server error</response>
        /// <returns>This is a return messsage</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseDto<CountryVM>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> AddCountry([FromBody] AddCountryCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailed)
            {
                return ToActionResult(result);
            }

            var message = result.Successes.FirstOrDefault()?.Message;

            return CreatedAtAction(nameof(GetById), new { id = result.Value.CountryId }, new ResponseDto<CountryVM>(result.Value, message));
        }


        /// <summary>
        /// Update an existing country.
        /// </summary>
        /// <param name="id">The country ID.</param>
        /// <param name="command">The updated country information.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ResponseDto<CountryVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> UpdateCountry([FromRoute, Range(1, int.MaxValue)] int id, [FromBody] UpdateCountryCommand command, CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await _mediator.Send(command, cancellationToken);

            return ToActionResult(result);
        }


        /// <summary>
        /// Soft-delete an existing country.
        /// </summary>
        /// <param name="id">The country ID.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCountry([FromRoute, Range(1, int.MaxValue)] int id, CancellationToken cancellationToken)
        {
            var command = new DeleteCountryCommand(id);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailed)
            {
                return ToActionResult(result);
            }

            return NoContent();
        }
    }
}
