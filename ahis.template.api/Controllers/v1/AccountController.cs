using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Features.AccountFeatures.Queries;
using ahis.template.api.Security;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AccountVM;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace ahis.template.api.Controllers.v1
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : BaseApiController
    {
        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <remarks>
        /// This endpoint registers a user using email or username.
        /// An email confirmation link will be sent after successful registration.
        /// </remarks>
        /// <param name="command">User registration data</param>
        /// <response code="204">User registered successfully with no response contents</response>
        /// <response code="400">Invalid registration request</response>
        /// <response code="500">Unexpected internal server error</response>
        [HttpPost("register")]
        [EnableRateLimiting("AnonymousAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return NoContent(); //NoContent return 204
        }

        /// <summary>
        /// Confirm user email
        /// </summary>
        /// <remarks>
        /// Confirms the user's email address using the token sent via email.
        /// </remarks>
        /// <param name="command">Email confirmation payload</param>
        /// <response code="204">Email confirmed successfully with no response contents</response>
        /// <response code="400">Invalid or expired confirmation token</response>
        /// <response code="500">Unexpected internal server error</response>
        [HttpPost("confirm-email")]
        [EnableRateLimiting("AnonymousAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return NoContent(); //NoContent return 204
        }

        /// <summary>
        /// Set user password
        /// </summary>
        /// <remarks>
        /// Sets a password with the short-lived setup token sent after email confirmation.
        /// </remarks>
        /// <param name="command">Password setup data</param>
        /// <response code="204">Password set successfully with no response contents</response>
        /// <response code="400">Invalid password format or request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Unexpected internal server error</response>
        [HttpPost("set-password")]
        [EnableRateLimiting("AnonymousAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return NoContent(); //NoContent return 204
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        /// <remarks>
        /// Updates the authenticated user's profile information.
        /// </remarks>
        /// <param name="command">Profile update data</param>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid profile data</response>
        /// <response code="500">Unexpected internal server error</response>
        [HttpPost("update-profile")]
        [ProducesResponseType(typeof(ResponseDto<ProfileUpdateResponseVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Generate authenticator setup
        /// </summary>
        /// <remarks>
        /// Generates QR code and setup key for enabling two-factor authentication (2FA).
        /// </remarks>
        /// <param name="command">Authenticator setup request</param>
        /// <response code="200">Authenticator setup generated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Unexpected internal server error</response>
        [HttpPost("generate-authenticator-setup")]
        [ProducesResponseType(typeof(ResponseDto<AuthenticatorSetupResponseVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        public async Task<IActionResult> GenerateAuthenticatorSetup()
        {
            var result = await _mediator.Send(new GenerateAuthenticatorSetupCommand());

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Enable two-factor authentication (2FA) using an authenticator app
        /// </summary>
        /// <remarks>
        /// This endpoint completes the two-factor authentication (2FA) setup process.
        /// <br/><br/>
        /// <b>Flow:</b>
        /// <ol>
        ///   <li>User scans the QR code generated from <c>generate-authenticator-setup</c></li>
        ///   <li>User enters the 6-digit code from their authenticator app</li>
        ///   <li>This endpoint verifies the code and enables 2FA for the user</li>
        /// </ol>
        /// <br/>
        /// <b>On success:</b>
        /// <ul>
        ///   <li>Two-factor authentication is enabled</li>
        ///   <li>A list of recovery codes is returned (must be shown once and saved securely by the user)</li>
        /// </ul>
        /// <br/>
        /// <b>Important:</b>
        /// <ul>
        ///   <li>Each recovery code can be used only once</li>
        ///   <li>If recovery codes are lost, the user may be locked out</li>
        /// </ul>
        /// </remarks>
        /// <param name="command">
        /// Payload containing a 6-digit authenticator verification code for the authenticated user.
        /// </param>
        /// <response code="200">Two-factor authentication enabled successfully</response>
        /// <response code="400">Invalid verification code or invalid request</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Unexpected internal server error</response>
        [HttpPost("enable-2fa")]
        [ProducesResponseType(typeof(ResponseDto<IEnumerable<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        public async Task<IActionResult> EnableAuthenticator([FromBody] EnableTwoFactorCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return ToActionResult(result);
        }


        /// <response code="204">Two-factor authentication enabled successfully</response>
        /// <response code="400">Invalid verification code or invalid request</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Unexpected internal server error</response>
        [HttpPost("disable-2fa")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        public async Task<IActionResult> DisableAuthenticator()
        {
            var result = await _mediator.Send(new DisableTwoFactorCommand());

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return NoContent();
        }


        /// <summary>
        /// Changes the password for the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// Security notes:
        /// - User must be authenticated
        /// - Current password must be valid
        /// - Security stamp is updated to invalidate other sessions
        /// </remarks>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            var result = await _mediator.Send(
                new ChangePasswordCommand(
                    command.CurrentPassword,
                    command.NewPassword));

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            return NoContent();
        }

        [HttpPost("re-authenticate")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Reauthenticate([FromBody] ReauthenticateCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailed)
                return ToValidationProblem(result);

            return Ok(new { stepUpProof = result.Value });
        }

        /// <summary>Revokes every refresh session for the current account.</summary>
        /// <remarks>
        /// Requires a current five-minute step-up proof. The caller's session is included,
        /// so the client must sign in again after this operation succeeds.
        /// </remarks>
        [HttpPost("sessions/revoke-all")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RevokeAllSessions([FromBody] RevokeAllSessionsCommand command)
        {
            var result = await _mediator.Send(command, HttpContext.RequestAborted);
            if (result.IsFailed)
            {
                if (result.Errors.Any(error => error.Metadata.ContainsKey("OperationalFailure")))
                    return Problem(statusCode: StatusCodes.Status500InternalServerError);

                return ToValidationProblem(result);
            }

            ClearRefreshCookie();
            return NoContent();
        }

        [HttpPost("2fa/reset-authenticator")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResetAuthenticator([FromBody] ResetAuthenticatorCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailed)
                return ToValidationProblem(result);

            ClearRefreshCookie();
            return NoContent();
        }

        [HttpPost("change-email/request")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RequestEmailChange([FromBody] RequestEmailChangeCommand command)
        {
            var result = await _mediator.Send(command);
            return result.IsSuccess ? NoContent() : ToValidationProblem(result);
        }

        [HttpPost("change-email/confirm")]
        [AllowAnonymous]
        [EnableRateLimiting("AnonymousAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeCommand command)
        {
            var result = await _mediator.Send(command);
            return result.IsSuccess ? NoContent() : ToValidationProblem(result);
        }

        [HttpPost("deactivate")]
        [Authorize]
        [EnableRateLimiting("AuthenticatedSecurityPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Deactivate([FromBody] DeactivateAccountCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailed)
                return ToValidationProblem(result);

            ClearRefreshCookie();
            return NoContent();
        }


        /// <summary>
        /// Resends the email confirmation link.
        /// </summary>
        /// <remarks>
        /// Security notes:
        /// - Response is always 204 to prevent user enumeration
        /// - Email is sent only if the account exists and is unconfirmed
        /// </remarks>
        [HttpPost("resend-confirmation-email")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("AnonymousAuthPolicy")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailed)
            {
                var modelState = new ModelStateDictionary();


                foreach (var error in result.Errors)
                {
                    if (error.Metadata.TryGetValue("Field", out var field))
                    {
                        modelState.AddModelError(field.ToString()!, error.Message);
                    }
                    else
                    {
                        modelState.AddModelError("general", error.Message);
                    }
                }

                return ValidationProblem(modelState);
            }

            // Always return 204
            return NoContent();
        }


        /// <summary>
        /// Gets the currently authenticated user's account information.
        /// </summary>
        /// <remarks>
        /// Security notes:
        /// - Requires valid access token
        /// - User identity is derived from JWT, not client input
        /// </remarks>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyAccount()
        {
            var result = await _mediator.Send(new GetMyAccountQuery());

            if (result.IsFailed)
                return Unauthorized();

            return Ok(result.Value);
        }

        private IActionResult ToValidationProblem(Result result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("general", error.Message);

            return ValidationProblem(ModelState);
        }

        private IActionResult ToValidationProblem<T>(Result<T> result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("general", error.Message);

            return ValidationProblem(ModelState);
        }

        private void ClearRefreshCookie()
        {
            RefreshCookie.Clear(Response);
        }



    }
}
