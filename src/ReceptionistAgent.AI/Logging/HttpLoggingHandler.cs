using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ReceptionistAgent.AI.Logging;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingHandler> _logger;

    public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("\n\n=== OUTGOING HTTP REQUEST ===\n[{Method}] {Uri}", request.Method, request.RequestUri);
        
        if (request.Content != null)
        {
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Request Body:\n{RequestBody}", requestBody);
        }

        var response = await base.SendAsync(request, cancellationToken);

        _logger.LogInformation("\n=== INCOMING HTTP RESPONSE ===\nStatus: {StatusCode}", response.StatusCode);

        if (response.Content != null)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Response Body:\n{ResponseBody}\n==============================\n\n", responseBody);
        }

        return response;
    }
}
