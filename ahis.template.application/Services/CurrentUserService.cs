using ahis.template.application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace ahis.template.application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        //public string? UserId =>
        //    _httpContextAccessor.HttpContext?.User?
        //        .FindFirstValue(ClaimTypes.NameIdentifier);


        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string? UserName => User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("name") ?? User?.FindFirstValue("preferred_username");

        public string? UserRole => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

        public string? IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context is null) return null;

                // Respect reverse-proxy / IIS forwarded headers (common in on-prem gov deployments)
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(forwardedFor))
                {
                    return forwardedFor.Split(',')[0].Trim();
                }
                    

                return context.Connection.RemoteIpAddress?.ToString();
            }
        }

        public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();

        public string? CorrelationId => _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}
