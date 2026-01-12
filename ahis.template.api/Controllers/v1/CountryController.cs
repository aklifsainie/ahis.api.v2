using ahis.template.application.Features.CountryFeatures.Command;
using ahis.template.application.Features.CountryFeatures.Query;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.CountryVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ahis.template.api.Controllers.v1
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        [HttpGet("get-all")]
        [ProducesResponseType(typeof(ResponseDto<List<CountryVM>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Produces("application/json")]
        public async Task<IActionResult> GetAll()
        {
            var query = new GetAllCountryQuery { };
            return Response(await _mediator.Send(query));

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
        [HttpPost("add")]
        [ProducesResponseType(typeof(ResponseDto<AddCountryCommand>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> AddCountry([FromBody] AddCountryCommand command)
        {
            return Response(await _mediator.Send(command));
        }
    }
}
