using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace ReceptionistAgent.Api.Security;

/// <summary>
/// Filtro de autorización que verifica el header X-Api-Key.
/// Permite proteger endpoints sensibles como la auditoría.
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

        if (validApiKeys == null || !validApiKeys.Contains(extractedApiKey.ToString()))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "API Key inválida." });
            return;
        }

        await next();
    }
}
