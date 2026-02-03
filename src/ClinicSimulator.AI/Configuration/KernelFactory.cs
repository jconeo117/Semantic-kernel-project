using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

namespace ClinicSimulator.AI.Configuration;
public class KernelFactory
{
    public static Kernel CreateKernel(IConfiguration configuration)
    {
        var endpoint = configuration["AI:LMStudio:Endpoint"];
        var ModelId = configuration["AI:LMStudio:ModelId"];

        var builder = Kernel.CreateBuilder();

        builder.Services.AddOpenAIChatCompletion(modelId: ModelId, apiKey: "LMStudio", endpoint: new Uri(endpoint));

        return builder.Build();
    }
}