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

    public async Task<ChatHistory> GetChatHistoryAsync(Guid sessionId, string tenantId, string systemPrompt)
    {
        const string sql = "SELECT HistoryJson FROM ChatSessions WHERE Id = @Id AND TenantId = @TenantId";

        using var connection = new SqlConnection(_connectionString);
        var json = await connection.QuerySingleOrDefaultAsync<string>(sql, new { Id = sessionId, TenantId = tenantId });

        if (string.IsNullOrWhiteSpace(json))
        {
            // If doesn't exist, start a new history with the system prompt
            var newHistory = new ChatHistory(systemPrompt);
            await InsertChatHistoryAsync(sessionId, tenantId, newHistory);
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

            // Ensure system prompt is present in restored history.
            // Serialization may lose it, and without it the LLM forgets its role.
            if (!history.Any(m => m.Role == AuthorRole.System))
            {
                history.Insert(0, new ChatMessageContent(AuthorRole.System, systemPrompt));
            }

            return history;
        }
        catch
        {
            // Fallback just in case JSON is corrupted
            return new ChatHistory(systemPrompt);
        }
    }

    public async Task UpdateChatHistoryAsync(Guid sessionId, string tenantId, ChatHistory history)
    {
        // Filter out tool-related messages before persisting.
        // Groq's strict validation rejects deserialized FunctionCall/FunctionResult
        // messages because the tool call format gets corrupted during JSON round-tripping.
        // We only persist system, user, and assistant (text-only) messages.
        var persistableMessages = history
            .Where(m => m.Role == AuthorRole.System
                     || m.Role == AuthorRole.User
                     || (m.Role == AuthorRole.Assistant && !string.IsNullOrEmpty(m.Content)))
            .Where(m => m.Items == null || !m.Items.Any(i =>
                i is Microsoft.SemanticKernel.FunctionCallContent
                || i is Microsoft.SemanticKernel.FunctionResultContent))
            .ToList();
        var json = JsonSerializer.Serialize(persistableMessages);

        const string sql = @"
            UPDATE ChatSessions 
            SET HistoryJson = @HistoryJson, UpdatedAt = @UpdatedAt 
            WHERE Id = @Id AND TenantId = @TenantId";

        using var connection = new SqlConnection(_connectionString);
        var rows = await connection.ExecuteAsync(sql, new
        {
            HistoryJson = json,
            UpdatedAt = DateTime.UtcNow,
            Id = sessionId,
            TenantId = tenantId
        });

        // If it didn't update anything, it means it doesn't exist anymore, let's insert it
        if (rows == 0)
        {
            await InsertChatHistoryAsync(sessionId, tenantId, history);
        }
    }

    private async Task InsertChatHistoryAsync(Guid sessionId, string tenantId, ChatHistory history)
    {
        var json = JsonSerializer.Serialize(history.ToList());
        const string sql = @"
            INSERT INTO ChatSessions (Id, TenantId, HistoryJson, CreatedAt, UpdatedAt)
            VALUES (@Id, @TenantId, @HistoryJson, @CreatedAt, @UpdatedAt)";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            Id = sessionId,
            TenantId = tenantId,
            HistoryJson = json,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
