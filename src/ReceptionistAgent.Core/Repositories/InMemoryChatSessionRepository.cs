using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ReceptionistAgent.Core.Repositories;

public class InMemoryChatSessionRepository : IChatSessionRepository
{
    private readonly ConcurrentDictionary<Guid, ChatHistory> _sessions = new();

    public Task<ChatHistory> GetChatHistoryAsync(Guid sessionId, string systemPrompt)
    {
        if (_sessions.TryGetValue(sessionId, out var history))
        {
            return Task.FromResult(history);
        }

        var newHistory = new ChatHistory(systemPrompt);
        _sessions.TryAdd(sessionId, newHistory);
        return Task.FromResult(newHistory);
    }

    public Task UpdateChatHistoryAsync(Guid sessionId, ChatHistory history)
    {
        _sessions.AddOrUpdate(sessionId, history, (key, oldValue) => history);
        return Task.CompletedTask;
    }
}
