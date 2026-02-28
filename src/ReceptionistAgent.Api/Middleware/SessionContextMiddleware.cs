using ReceptionistAgent.Core.Session;
using Microsoft.AspNetCore.Http;

namespace ReceptionistAgent.Api.Middleware;

/// <summary>
/// Middleware que inicializa el SessionContext por request.
/// Si el header X-Client-Id está presente, pre-valida el paciente en la sesión.
/// </summary>
public class SessionContextMiddleware
{
    private readonly RequestDelegate _next;
    private const string ClientIdHeaderName = "X-Client-Id";

    public SessionContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionContext sessionContext)
    {
        // Permitir Swagger y health checks sin validación
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/swagger") || path.StartsWith("/health"))
        {
            await _next(context);
            return;
        }

        // Si el header X-Client-Id está presente, pre-validar el paciente
        if (context.Request.Headers.TryGetValue(ClientIdHeaderName, out var clientId) &&
            !string.IsNullOrWhiteSpace(clientId))
        {
            sessionContext.ValidateClientId(clientId!);
        }

        await _next(context);
    }
}
