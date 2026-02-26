using ClinicSimulator.Core.Session;
using Microsoft.AspNetCore.Http;

namespace ClinicSimulator.Api.Middleware;

/// <summary>
/// Middleware que inicializa el SessionContext por request.
/// Si el header X-Patient-Id está presente, pre-valida el paciente en la sesión.
/// </summary>
public class SessionContextMiddleware
{
    private readonly RequestDelegate _next;
    private const string PatientIdHeaderName = "X-Patient-Id";

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

        // Si el header X-Patient-Id está presente, pre-validar el paciente
        if (context.Request.Headers.TryGetValue(PatientIdHeaderName, out var patientId) &&
            !string.IsNullOrWhiteSpace(patientId))
        {
            sessionContext.ValidatePatientId(patientId!);
        }

        await _next(context);
    }
}
