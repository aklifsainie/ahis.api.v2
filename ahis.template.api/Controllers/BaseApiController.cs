using FluentResults;
using ahis.template.application.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ahis.template.application.Shared.Errors;


namespace ahis.template.api.Controllers
{
    public partial class BaseApiController : ControllerBase
    {
        protected new IActionResult Response<T>(
            Result<T> result)
        {
            // Handle null result
            if (result is null ||
                (result.IsSuccess && result.ValueOrDefault is null))
                return HandleNullProblem();

            // Handle success result
            if (result.IsSuccess)
            {
                string? message = result.Successes.FirstOrDefault()?.Message;
                return Ok(new ResponseDto<T>(result.ValueOrDefault, message));
            }

            // Handle FluentValidation errors
            if (result.Errors.Any(x => x is ValidationError))
                return HandleValidationProblem(result);

            // Handle FluentResult errors
            return HandleFluentResultProblem(result);
        }

        public IActionResult PagedResponse<T>(
            Result<PagedResult<T>> result)
        {
            // Handle null result
            if (result is null ||
                (result.IsSuccess && result.ValueOrDefault is null))
                return HandleNullProblem();

            // Handle success result
            if (result.IsSuccess)
                return Ok(new PagedResponseDto<T>(
                result.Value.Data,
                result.Value.TotalCount,
                result.Value.PageNumber,
                result.Value.PageSize
                ));

            // Handle FluentValidation errors
            if (result.Errors.Any(x => x is ValidationError))
                return HandleValidationProblem(result);

            // Handle FluentResult errors
            return HandleFluentResultProblem(result);
        }

        protected IActionResult UnhandledProblem()
        {
            Exception? ex = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
            int statusCode = StatusCodes.Status500InternalServerError;

            return Problem(
                    detail: ex?.Message ?? "An error has occurred.",
                    statusCode: statusCode);
        }

        private IActionResult HandleNullProblem()
        {
            int statusCode = StatusCodes.Status404NotFound;

            return Problem(
                statusCode: statusCode);
        }

        private IActionResult HandleValidationProblem<T>(Result<T> result)
        {
            foreach (KeyValuePair<string, object> metadata in result.Errors.SelectMany(e => e.Metadata))
            {
                ModelState.TryAddModelError(metadata.Key, metadata.Value?.ToString() ?? $"{metadata.Key} is invalid.");
            }

            return ValidationProblem(
                detail: "One or more validation errors occurred.",
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        private IActionResult HandleFluentResultProblem<T>(Result<T> result)
        {
            IError? firstError = result.Errors.FirstOrDefault();

            // Unhandled errors
            if (firstError is null)
                return UnhandledProblem();

            // Handle FluentResult errors
            int statusCode = firstError switch
            {
                ValidationError => StatusCodes.Status400BadRequest,
                EntityNotFoundError => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            return Problem(
                detail: firstError.Message,
                statusCode: statusCode);
        }
    }
}
