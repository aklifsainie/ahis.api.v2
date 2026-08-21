using ahis.template.application.Interfaces.Validators;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;


namespace ahis.template.api.ApiClientAuthentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOption>
    {
        private readonly IApiKeyValidator _apiKeyValidator;
        private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

        public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOption> options, ILoggerFactory loggerFactory, UrlEncoder encoder, IApiKeyValidator apiKeyValidator) : base(options, loggerFactory, encoder)
        {
            _apiKeyValidator = apiKeyValidator;

            _logger = loggerFactory.CreateLogger<ApiKeyAuthenticationHandler>();
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefault.HeaderName, out var headerValues))
            {
                return AuthenticateResult.NoResult();
            }

            if (headerValues.Count != 1)
            {
                return AuthenticateResult.Fail("Only one API key header is allowed.");
            }

            var rawApiKey = headerValues.ToString().Trim();

            if (string.IsNullOrWhiteSpace(rawApiKey))
            {
                return AuthenticateResult.Fail("The API key cannot be empty.");
            }

            var validationResult = await _apiKeyValidator.ValidateAsync(rawApiKey, Context.RequestAborted);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("API-key authentication failed. Error code: {ErrorCode}", validationResult.ErrorCode);

                /*
                 * Do not reveal whether the key was expired,
                 * revoked or unknown.
                 */
                return AuthenticateResult.Fail("Invalid API key.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, validationResult.ClientId!),
                new(ClaimTypes.Name, validationResult.ClientName!),
                new(ApiKeyClaimType.ClientId, validationResult.ClientId!),
                new(ApiKeyClaimType.ClientName, validationResult.ClientName!),
                new(ApiKeyClaimType.ApiClientDatabaseId, validationResult.ApiClientDatabaseId!.Value.ToString()),
                new(ApiKeyClaimType.ApiClientKeyId, validationResult.ApiClientKeyId!.Value.ToString()),
                new(ApiKeyClaimType.AuthenticationType, "api_key")
            };

            foreach (var permission in validationResult.Permissions)
            {
                claims.Add(new Claim(ApiKeyClaimType.Permission, permission));
            }

            var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefault.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationDefault.AuthenticationScheme);

            /*
             * For a low-volume implementation.
             * Consider background updates for high traffic.
             */
            await _apiKeyValidator.UpdateLastUsedAsync(validationResult.ApiClientKeyId.Value, Context.RequestAborted);

            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;

            Response.ContentType = "application/problem+json";

            await Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    title = "Unauthorized",
                    status = StatusCodes.Status401Unauthorized,
                    detail = "A valid API key is required.",
                    traceId = Context.TraceIdentifier
                })
            );
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;

            Response.ContentType = "application/problem+json";

            await Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    title = "Forbidden",
                    status = StatusCodes.Status403Forbidden,
                    detail = "The API client does not have permission " + "to access this resource.",
                    traceId = Context.TraceIdentifier
                })
            );
        }
    }
}
