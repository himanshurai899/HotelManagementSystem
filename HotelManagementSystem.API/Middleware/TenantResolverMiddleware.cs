using System.Security.Claims;

namespace HotelManagementSystem.API.Middleware
{
    public class TenantResolverMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Try JWT claim first
            var tenantIdClaim = context.User?.FindFirst("TenantId")?.Value;

            if (!string.IsNullOrWhiteSpace(tenantIdClaim) && int.TryParse(tenantIdClaim, out var tenantIdFromJwt))
            {
                context.Items["TenantId"] = tenantIdFromJwt;
            }
            else
            {
                // 2. Fall back to X-Tenant-Id header
                if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue)
                    && int.TryParse(headerValue, out var tenantIdFromHeader))
                {
                    context.Items["TenantId"] = tenantIdFromHeader;
                }
            }

            await _next(context);
        }
    }
}
