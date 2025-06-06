using System.Security.Claims;
using webprogbackend.Models.Enums;

namespace webprogbackend.Middleware
{
    public class RoleAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RoleAuthorizationMiddleware> _logger;

        public RoleAuthorizationMiddleware(RequestDelegate next, ILogger<RoleAuthorizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Authenticated user için rol kontrolü
            if (context.User.Identity.IsAuthenticated)
            {
                var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value ??
                               context.User.FindFirst("Role")?.Value;

                if (!string.IsNullOrEmpty(roleClaim))
                {
                    // Role claim'ini UserRole enum'a çevir
                    if (Enum.TryParse<UserRole>(roleClaim, out var userRole))
                    {
                        // Kullanıcının rolünü context'e ekle
                        context.Items["UserRole"] = userRole;
                        context.Items["IsAdmin"] = userRole == UserRole.Admin;
                        context.Items["IsUser"] = userRole == UserRole.User;

                        _logger.LogDebug($"User role set: {userRole} for user: {context.User.Identity.Name}");
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid role claim: {roleClaim} for user: {context.User.Identity.Name}");
                    }
                }
                else
                {
                    _logger.LogWarning($"No role claim found for user: {context.User.Identity.Name}");
                }
            }

            await _next(context);
        }
    }

    // Extension method for easier middleware registration
    public static class RoleAuthorizationMiddlewareExtensions
    {
        public static IApplicationBuilder UseRoleAuthorization(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RoleAuthorizationMiddleware>();
        }
    }
}