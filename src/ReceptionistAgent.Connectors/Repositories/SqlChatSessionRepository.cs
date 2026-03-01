using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.Collections.Concurrent;

namespace ReceptionistAgent.Connectors.Repositories;

public class SqlChatSessionRepository : IChatSessionRepository
{
    private readonly string _connectionString;

    public SqlChatSessionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ChatHistory> GetChatHistoryAsync(Guid sessionId, string systemPrompt)
    {
        const string sql = "SELECT HistoryJson FROM ChatSessions WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        var json = await connection.QuerySingleOrDefaultAsync<string>(sql, new { Id = sessionId });

        if (string.IsNullOrWhiteSpace(json))
        {
            // If doesn't exist, start a new history with the system prompt
            var newHistory = new ChatHistory(systemPrompt);
            await InsertChatHistoryAsync(sessionId, newHistory);
            return newHistory;
        }

        // Deserialize the actual ChatHistory
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var messages = JsonSerializer.Deserialize<List<ChatMessageContent>>(json, options);

            var history = new ChatHistory();
            if (messages != null)
            {
                foreach (var msg in messages)
                {
                    history.Add(msg);
                }
            }

            return history;
        }
        catch
        {
            // Fallback just in case JSON is corrupted
            return new ChatHistory(systemPrompt);
        }
    }

    public async Task UpdateChatHistoryAsync(Guid sessionId, ChatHistory history)
    {
        var messages = history.ToList(); // Convert to serializable List<ChatMessageContent>
        var json = JsonSerializer.Serialize(messages);

        const string sql = @"
            UPDATE ChatSessions 
            SET HistoryJson = @HistoryJson, UpdatedAt = @UpdatedAt 
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        var rows = await connection.ExecuteAsync(sql, new
        {
            HistoryJson = json,
            UpdatedAt = DateTime.UtcNow,
            Id = sessionId
        });

        // If it didn't update anything, it means it doesn't exist anymore, let's insert it
        if (rows == 0)
        {
            await InsertChatHistoryAsync(sessionId, history);
        }
    }

    private async Task InsertChatHistoryAsync(Guid sessionId, ChatHistory history)
    {
        var json = JsonSerializer.Serialize(history.ToList());
        const string sql = @"
            INSERT INTO ChatSessions (Id, HistoryJson, CreatedAt, UpdatedAt)
            VALUES (@Id, @HistoryJson, @CreatedAt, @UpdatedAt)";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            Id = sessionId,
            HistoryJson = json,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
