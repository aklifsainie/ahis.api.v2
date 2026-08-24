using FluentResults;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace ahis.template.api.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult ToActionResult<T>(Result<T>? result)
        {
            if (result is null)
            {
                return InternalServerProblem();
            }

            if (result.IsSuccess)
            {
                if (result.ValueOrDefault is null)
                {
                    return InternalServerProblem();
                }

                var message = result.Successes
                    .FirstOrDefault()?
                    .Message;

                return Ok(new ResponseDto<T>(
                    result.ValueOrDefault,
                    message));
            }

            if (result.Errors.Any(error => error is ValidationError))
            {
                return HandleValidationProblem(result);
            }

            return HandleResultProblem(result);
        }

        protected IActionResult ToPagedActionResult<T>(
            Result<PagedResult<T>>? result)
        {
            if (result is null)
            {
                return InternalServerProblem();
            }

            if (result.IsSuccess)
            {
                if (result.ValueOrDefault is null)
                {
                    return InternalServerProblem();
                }

                var pagedResult = result.Value;

                return Ok(new PagedResponseDto<T>(
                    pagedResult.Data,
                    pagedResult.TotalCount,
                    pagedResult.PageNumber,
                    pagedResult.PageSize));
            }

            if (result.Errors.Any(error => error is ValidationError))
            {
                return HandleValidationProblem(result);
            }

            return HandleResultProblem(result);
        }

        private IActionResult HandleValidationProblem<T>(Result<T> result)
        {
            foreach (var metadata in result.Errors
                         .OfType<ValidationError>()
                         .SelectMany(error => error.Metadata))
            {
                ModelState.TryAddModelError(
                    metadata.Key,
                    metadata.Value?.ToString()
                    ?? $"{metadata.Key} is invalid.");
            }

            return ValidationProblem(
                detail: "One or more validation errors occurred.",
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        private IActionResult HandleResultProblem<T>(Result<T> result)
        {
            var error = result.Errors.FirstOrDefault();

            return error switch
            {
                EntityNotFoundError notFoundError =>
                    Problem(
                        title: "Resource not found",
                        detail: notFoundError.Message,
                        statusCode: StatusCodes.Status404NotFound),

                ConflictError conflictError =>
                    Problem(
                        title: "Conflict",
                        detail: conflictError.Message,
                        statusCode: StatusCodes.Status409Conflict),

                _ => InternalServerProblem()
            };
        }

        private IActionResult InternalServerProblem()
        {
            return Problem(
                title: "Internal server error",
                detail: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}