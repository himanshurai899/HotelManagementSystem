using HotelManagementSystem.Shared.Data;
using HotelManagementSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagementSystem.API.Middleware
{
    public class TenantResolverMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, IServiceProvider services)
        {
            int? resolvedTenantId = null;

            // 1. Try JWT claim first
            var tenantIdClaim = context.User?.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrWhiteSpace(tenantIdClaim) && int.TryParse(tenantIdClaim, out var fromJwt))
                resolvedTenantId = fromJwt;

            // 2. SuperAdmin (or any caller) may override via X-Tenant-Id header —
            //    useful when SuperAdmin selects a specific tenant from the UI.
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue)
                && int.TryParse(headerValue, out var fromHeader))
                resolvedTenantId = fromHeader;   // header always wins — allows SuperAdmin override

            if (resolvedTenantId.HasValue)
            {
                context.Items["TenantId"] = resolvedTenantId.Value;

                // Load and cache the full Tenant object once per request so controllers
                // and services can access TenantName, CurrencyCode, Locale etc. without
                // issuing a separate DB query each time.
                // Uses a scoped IServiceProvider to safely resolve the scoped DbContext
                // from within this singleton-lifetime middleware.
                using var scope  = services.CreateScope();
                var db           = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                var tenant       = await db.Tenants
                                           .AsNoTracking()
                                           .FirstOrDefaultAsync(t => t.Id == resolvedTenantId.Value);
                if (tenant is not null)
                    context.Items["Tenant"] = tenant;
            }

            await _next(context);
        }
    }
}

