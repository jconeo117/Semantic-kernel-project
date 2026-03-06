using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ReceptionistAgent.Api.Security;

/// <summary>
/// Filtro de autorización que verifica el header X-Api-Key.
/// Permite proteger endpoints sensibles como la auditoría.
/// Usa comparación timing-safe para prevenir timing attacks.
/// </summary>
public class ApiKeyAuthFilter : IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { error = $"API Key ausente. Use el header {ApiKeyHeaderName}" });
            return;
        }

        var validApiKeys = _configuration.GetSection("Security:ApiKeys").Get<List<string>>();

        if (validApiKeys == null || !IsValidApiKey(extractedApiKey.ToString(), validApiKeys))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "API Key inválida." });
            return;
        }

        await next();
    }

    /// <summary>
    /// Timing-safe API key comparison to prevent timing attacks.
    /// </summary>
    private static bool IsValidApiKey(string provided, List<string> validKeys)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        foreach (var key in validKeys)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (providedBytes.Length == keyBytes.Length &&
                CryptographicOperations.FixedTimeEquals(providedBytes, keyBytes))
            {
                return true;
            }
        }
        return false;
    }
}
