using Microsoft.SemanticKernel.ChatCompletion;

namespace ReceptionistAgent.Core.Repositories;

public interface IChatSessionRepository
{
    Task<ChatHistory> GetChatHistoryAsync(Guid sessionId, string systemPrompt);
    Task UpdateChatHistoryAsync(Guid sessionId, ChatHistory history);
}
