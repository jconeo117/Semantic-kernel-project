using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Tenant;
using Microsoft.AspNetCore.Http;

namespace ReceptionistAgent.Api.Middleware;

/// <summary>
/// Middleware que resuelve el tenant a partir del header X-Tenant-Id.
/// Si el header no está presente o el tenant no existe, retorna 400.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private const string TenantHeaderName = "X-Tenant-Id";

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, TenantContext tenantContext)
    {
        // Permitir Swagger y health checks sin tenant
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/swagger") || path.StartsWith("/health") || path.StartsWith("/api/audit"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(TenantHeaderName, out var tenantId) ||
            string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"error\":\"Header 'X-Tenant-Id' es requerido.\"}");
            return;
        }

        var tenant = await tenantResolver.ResolveAsync(tenantId!);

        if (tenant == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var allTenants = await tenantResolver.GetAllTenantIdsAsync();
            await context.Response.WriteAsync(
                $"{{\"error\":\"Tenant '{tenantId}' no encontrado.\",\"available\":[{string.Join(",", allTenants.Select(t => $"\"{t}\""))}]}}");
            return;
        }

        tenantContext.CurrentTenant = tenant;
        await _next(context);
    }
}
