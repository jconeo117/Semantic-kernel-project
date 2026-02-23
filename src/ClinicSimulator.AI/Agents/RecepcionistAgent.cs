using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ClinicSimulator.AI.Agents;

public class RecepcionistAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _ChatCompletionService;
    private readonly string _provider;

    public RecepcionistAgent(Kernel kernel, string provider)
    {
        _kernel = kernel;
        _ChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        _provider = provider;
    }

    public async Task<string> RespondAsync(string UserMessage, ChatHistory chatHistory)
    {
        chatHistory.AddUserMessage(UserMessage);
        PromptExecutionSettings? settings = null;

        if (_provider == "Google")
        {
            settings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.7,
                MaxTokens = 500
            };

        }
        else if (_provider == "GROQ")
        {
            settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.7,
                MaxTokens = 500
            };
        }

        var result = await _ChatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel
        );

        chatHistory.AddAssistantMessage(result.Content ?? string.Empty);

        return result.Content ?? "Lo siento, no pude procesar su solicitud.";
    }
}