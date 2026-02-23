using Microsoft.SemanticKernel.ChatCompletion;

namespace ClinicSimulator.Core.Repositories;

public interface IChatSessionRepository
{
    Task<ChatHistory> GetChatHistoryAsync(Guid sessionId, string systemPrompt);
    Task UpdateChatHistoryAsync(Guid sessionId, ChatHistory history);
}
