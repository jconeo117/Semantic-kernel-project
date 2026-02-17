using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.Extensions.Configuration;
using ClinicSimulator.AI.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSimulator.AI.Configuration;
public class KernelFactory
{
    public static Kernel CreateKernel(IConfiguration configuration, string provider)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionInvocationFilter>();


        if (provider == "Google")
        {
            var modelId = configuration["AI:Google:ModelId"]!;
            var apiKey = configuration["AI:Google:ApiKey"]!;
            builder.Services.AddGoogleAIGeminiChatCompletion(
                modelId: "gemini-robotics-er-1.5-preview"!,
                apiKey: apiKey!
            );
        }
        else if (provider == "GROQ")
        {
            var endpoint = configuration["AI:GROQ:Endpoint"]!;
            var modelId = configuration["AI:GROQ:ModelId"]!;
            var apiKey = configuration["AI:GROQ:ApiKey"] ?? "LMStudio";

            // AddOpenAIChatCompletion is also an extension on IKernelBuilder usually, using that for consistency if possible, 
            // but if the previous code used Services.Add... validly, I might keep it. 
            // However, builder.AddOpenAIChatCompletion is the standard SK way.
            builder.AddOpenAIChatCompletion(modelId: modelId, apiKey: apiKey, endpoint: new Uri(endpoint));
        }

        return builder.Build();
    }
}