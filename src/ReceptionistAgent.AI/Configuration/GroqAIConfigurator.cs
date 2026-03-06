using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ReceptionistAgent.AI.Configuration;

/// <summary>
/// Configurador para GROQ y endpoints compatibles con OpenAI
/// (LM Studio, Ollama, vLLM, etc.)
/// Lee Endpoint, ModelId y ApiKey desde la sección AI:GROQ del appsettings.
/// </summary>
public class GroqAIConfigurator : IAIProviderConfigurator
{
    public string ProviderName => "GROQ";

    private int _maxTokens = 1500;

    public void ConfigureKernel(IKernelBuilder builder, IConfiguration configuration)
    {
        var endpoint = configuration["AI:GROQ:Endpoint"]
            ?? throw new InvalidOperationException("AI:GROQ:Endpoint is required in configuration.");
        var modelId = configuration["AI:GROQ:ModelId"]
            ?? throw new InvalidOperationException("AI:GROQ:ModelId is required in configuration.");
        var apiKey = configuration["AI:GROQ:ApiKey"] ?? "LMStudio";

        if (int.TryParse(configuration["AI:GROQ:MaxTokens"], out var tokens))
        {
            _maxTokens = tokens;
        }

        var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
        var handler = new ReceptionistAgent.AI.Logging.HttpLoggingHandler(loggerFactory.CreateLogger<ReceptionistAgent.AI.Logging.HttpLoggingHandler>(), new System.Net.Http.HttpClientHandler());
        var httpClient = new System.Net.Http.HttpClient(handler);

        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            endpoint: new Uri(endpoint),
            httpClient: httpClient
        );
    }

    public PromptExecutionSettings CreateExecutionSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.7,
            MaxTokens = _maxTokens
        };
    }
}
