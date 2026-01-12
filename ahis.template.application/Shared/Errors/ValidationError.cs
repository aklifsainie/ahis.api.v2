using FluentResults;

namespace ahis.template.application.Shared.Errors
{
    public class ValidationError : Error
    {
        /// <summary>
        /// HTTP status code 400.
        /// </summary>
        /// <param name="message"></param>
        public ValidationError(string? message = "One or more validation errors occurred.") : base(message)
        {
        }
    }
}
