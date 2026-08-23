using ahis.template.api.ApiClientAuthentication;
using ahis.template.application.Features.AuditLogFeatures.Queries;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AuditLogVM;
using ahis.template.domain.Models.ViewModels.CommonVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ahis.template.api.Controllers
{
    [Route("api/auditlog")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer," + ApiKeyAuthenticationDefault.AuthenticationScheme)]
    [EnableRateLimiting("ApiPolicy")]
    public class AuditLogController : BaseApiController
    {
        private readonly IMediator _mediator;

        public AuditLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// GET /api/audit-logs?entityName=Complaint&amp;entityId=...&amp;fromUtc=...&amp;toUtc=...&amp;pageNumber=1&amp;pageSize=20
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<AuditLogVM>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<AuditLogVM>>> GetAuditLogs([FromQuery] GetAuditLogQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }


        /// <summary>
        /// GET /api/audit-logs/entity/Complaint/3f2504e0-4f89-11d3-9a0c-0305e82c3301
        /// Convenience endpoint: full history for one specific record, most recent first.
        /// </summary>
        [HttpGet("entity/{entityName}/{entityId}")]
        [ProducesResponseType(typeof(PaginatedResult<AuditLogVM>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<AuditLogVM>>> GetHistoryForEntity(string entityName, string entityId, [FromQuery] int pageNumber, [FromQuery] int pageSize)
        {
            var query = new GetAuditLogQuery
            {
                EntityName = entityName,
                EntityId = entityId,
                PageNumber = pageNumber == 0 ? 1 : pageNumber,
                PageSize = pageSize == 0 ? 20 : pageSize
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
