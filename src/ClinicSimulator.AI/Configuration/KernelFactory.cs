using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using ClinicSimulator.AI.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSimulator.AI.Configuration;
public class KernelFactory
{
    public static Kernel CreateKernel(IConfiguration configuration)
    {
        var endpoint = configuration["AI:LMStudio:Endpoint"];
        var ModelId = configuration["AI:LMStudio:ModelId"];

        var builder = Kernel.CreateBuilder();

        builder.Services.AddOpenAIChatCompletion(modelId: ModelId!, apiKey: "LMStudio", endpoint: new Uri(endpoint!));

        builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionInvocationFilter>();


        return builder.Build();
    }
}