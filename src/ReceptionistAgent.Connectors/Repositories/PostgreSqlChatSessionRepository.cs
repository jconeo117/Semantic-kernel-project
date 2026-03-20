using Dapper;
using Npgsql;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using ReceptionistAgent.Core.Session;
using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Connectors.Repositories;

public class PostgreSqlChatSessionRepository : IChatSessionRepository
{
    private readonly string _agentCoreConnectionString;
    private readonly string? _tenantConnectionString;

    public PostgreSqlChatSessionRepository(string agentCoreConnectionString, string? tenantConnectionString = null)
    {
        _agentCoreConnectionString = agentCoreConnectionString;
        _tenantConnectionString = tenantConnectionString;
        _isCorePostgres = agentCoreConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);
    }
    private readonly bool _isCorePostgres;

    public async Task<ChatHistory> GetChatHistoryAsync(Guid sessionId, string tenantId, string systemPrompt, string? userPhone = null)
    {
        // Primary: Tenant DB (PostgreSQL)
        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            try
            {
                const string sql = "SELECT history_json FROM chat_sessions WHERE id = @Id";
                using var connection = new NpgsqlConnection(_tenantConnectionString);
                var json = await connection.QuerySingleOrDefaultAsync<string>(sql, new { Id = sessionId });

                if (!string.IsNullOrWhiteSpace(json))
                {
                    return MapToHistory(json, systemPrompt);
                }
            }
            catch
            {
                // Fallback to AgentCore if tenant DB fails or record not found
            }
        }

        // Fallback: AgentCore
        string coreSql = _isCorePostgres
            ? "SELECT history_json FROM chat_sessions WHERE id = @Id"
            : "SELECT HistoryJson FROM ChatSessions WHERE Id = @Id AND TenantId = @TenantId";

        using (var connection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString))
        {
            var json = await connection.QuerySingleOrDefaultAsync<string>(coreSql, new { Id = sessionId, TenantId = tenantId });

            if (!string.IsNullOrWhiteSpace(json))
            {
                return MapToHistory(json, systemPrompt);
            }
        }

        // New history
        var newHistory = new ChatHistory(systemPrompt);
        await InsertChatHistoryAsync(sessionId, tenantId, newHistory, userPhone);
        return newHistory;
    }

    private static ChatHistory MapToHistory(string json, string systemPrompt)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var messages = JsonSerializer.Deserialize<List<ChatMessageContent>>(json, options);

            var history = new ChatHistory();
            if (messages != null)
            {
                foreach (var msg in messages.Where(m => m.Role != AuthorRole.System))
                {
                    history.Add(msg);
                }
            }
            history.Insert(0, new ChatMessageContent(AuthorRole.System, systemPrompt));
            return history;
        }
        catch
        {
            return new ChatHistory(systemPrompt);
        }
    }

    public async Task UpdateChatHistoryAsync(Guid sessionId, string tenantId, ChatHistory history, string? userPhone = null)
    {
        var persistableMessages = history
            .Where(m => m.Role == AuthorRole.System
                     || m.Role == AuthorRole.User
                     || (m.Role == AuthorRole.Assistant && !string.IsNullOrEmpty(m.Content)))
            .Where(m => m.Items == null || !m.Items.Any(i =>
                i is Microsoft.SemanticKernel.FunctionCallContent
                || i is Microsoft.SemanticKernel.FunctionResultContent))
            .ToList();
        var json = JsonSerializer.Serialize(persistableMessages);

        // 1. Primary Write (Tenant DB - PostgreSQL)
        bool updatedInTenant = false;
        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            const string tenantSql = "UPDATE chat_sessions SET history_json = @HistoryJson::jsonb, updated_at = @UpdatedAt WHERE id = @Id";
            using var connection = new NpgsqlConnection(_tenantConnectionString);
            var rows = await connection.ExecuteAsync(tenantSql, new
            {
                HistoryJson = json,
                UpdatedAt = DateTime.UtcNow,
                Id = sessionId
            });
            updatedInTenant = rows > 0;
        }

        // 2. Backup Write (AgentCore) - Fire & Forget
        _ = Task.Run(async () =>
        {
            try
            {
                string coreSql = _isCorePostgres
                    ? "UPDATE chat_sessions SET history_json = @HistoryJson::jsonb, updated_at = @UpdatedAt WHERE id = @Id"
                    : "UPDATE ChatSessions SET HistoryJson = @HistoryJson, UpdatedAt = @UpdatedAt WHERE Id = @Id AND TenantId = @TenantId";

                using var connection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
                var rows = await connection.ExecuteAsync(coreSql, new
                {
                    HistoryJson = json,
                    UpdatedAt = DateTime.UtcNow,
                    Id = sessionId,
                    TenantId = tenantId
                });

                if (rows == 0 && !updatedInTenant)
                {
                    await InsertChatHistoryAsync(sessionId, tenantId, history, userPhone);
                }
            }
            catch { }
        });

        if (!updatedInTenant && !string.IsNullOrEmpty(_tenantConnectionString))
        {
            await InsertChatHistoryAsync(sessionId, tenantId, history, userPhone);
        }
    }

    private async Task InsertChatHistoryAsync(Guid sessionId, string tenantId, ChatHistory history, string? userPhone)
    {
        var json = JsonSerializer.Serialize(history.ToList());
        var now = DateTime.UtcNow;

        // 1. Primary: Tenant DB (PostgreSQL)
        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            try
            {
                const string tenantSql = @"
                    INSERT INTO chat_sessions (id, user_phone, history_json, needs_human_attention, created_at, updated_at)
                    VALUES (@Id, @UserPhone, @HistoryJson::jsonb, false, @CreatedAt, @UpdatedAt)
                    ON CONFLICT (id) DO UPDATE SET 
                        history_json = EXCLUDED.history_json, 
                        updated_at = EXCLUDED.updated_at";

                using var connection = new NpgsqlConnection(_tenantConnectionString);
                await connection.ExecuteAsync(tenantSql, new
                {
                    Id = sessionId,
                    UserPhone = userPhone ?? string.Empty,
                    HistoryJson = json,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            catch { }
        }

        // 2. Backup: AgentCore
        try
        {
            string coreSql;
            if (_isCorePostgres)
            {
                coreSql = @"
                    INSERT INTO chat_sessions (id, user_phone, history_json, needs_human_attention, created_at, updated_at)
                    VALUES (@Id, @UserPhone, @HistoryJson::jsonb, false, @CreatedAt, @UpdatedAt)
                    ON CONFLICT (id) DO UPDATE SET 
                        history_json = EXCLUDED.history_json, 
                        updated_at = EXCLUDED.updated_at";
            }
            else
            {
                coreSql = @"
                    IF NOT EXISTS (SELECT 1 FROM ChatSessions WHERE Id = @Id)
                    BEGIN
                        INSERT INTO ChatSessions (Id, TenantId, UserPhone, HistoryJson, NeedsHumanAttention, CreatedAt, UpdatedAt)
                        VALUES (@Id, @TenantId, @UserPhone, @HistoryJson, 0, @CreatedAt, @UpdatedAt)
                    END
                    ELSE
                    BEGIN
                        UPDATE ChatSessions SET HistoryJson = @HistoryJson, UpdatedAt = @UpdatedAt 
                        WHERE Id = @Id AND TenantId = @TenantId
                    END";
            }

            using var coreConnection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
            await coreConnection.ExecuteAsync(coreSql, new
            {
                Id = sessionId,
                TenantId = tenantId,
                UserPhone = userPhone ?? string.Empty,
                HistoryJson = json,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        catch { }
    }

    public async Task SetNeedsHumanAttentionAsync(Guid sessionId, string tenantId, bool needsAttention)
    {
        // 1. Primary: Tenant DB (PostgreSQL)
        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            const string tenantSql = "UPDATE chat_sessions SET needs_human_attention = @NeedsAttention, updated_at = @UpdatedAt WHERE id = @Id";
            using var connection = new NpgsqlConnection(_tenantConnectionString);
            await connection.ExecuteAsync(tenantSql, new
            {
                NeedsAttention = needsAttention,
                UpdatedAt = DateTime.UtcNow,
                Id = sessionId
            });
        }

        // 2. Backup: AgentCore - Fire & Forget
        _ = Task.Run(async () =>
        {
            try
            {
                string coreSql = _isCorePostgres
                    ? "UPDATE chat_sessions SET needs_human_attention = @NeedsAttention, updated_at = @UpdatedAt WHERE id = @Id"
                    : "UPDATE ChatSessions SET NeedsHumanAttention = @NeedsAttention, UpdatedAt = @UpdatedAt WHERE Id = @Id AND TenantId = @TenantId";

                using var connection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
                await connection.ExecuteAsync(coreSql, new
                {
                    NeedsAttention = _isCorePostgres ? (object)needsAttention : (object)(needsAttention ? 1 : 0),
                    UpdatedAt = DateTime.UtcNow,
                    Id = sessionId,
                    TenantId = tenantId
                });
            }
            catch { }
        });
    }

    public async Task<List<ReceptionistAgent.Core.Models.ChatSessionDto>> GetActiveSessionsAsync(string tenantId)
    {
        // Primary: Tenant DB (PostgreSQL)
        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            try
            {
                const string sql = @"
                    SELECT id as Id, user_phone as UserPhone, needs_human_attention as NeedsHumanAttention, created_at as CreatedAt, updated_at as UpdatedAt 
                    FROM chat_sessions 
                    ORDER BY updated_at DESC";

                using var connection = new NpgsqlConnection(_tenantConnectionString);
                var sessions = await connection.QueryAsync<ReceptionistAgent.Core.Models.ChatSessionDto>(sql);
                return sessions.Select(s => { s.TenantId = tenantId; return s; }).ToList();
            }
            catch { }
        }

        // Fallback: AgentCore
        string coreSql = _isCorePostgres
            ? "SELECT id as Id, user_phone as UserPhone, needs_human_attention as NeedsHumanAttention, created_at as CreatedAt, updated_at as UpdatedAt FROM chat_sessions ORDER BY updated_at DESC"
            : "SELECT Id, TenantId, UserPhone, NeedsHumanAttention, CreatedAt, UpdatedAt FROM ChatSessions WHERE TenantId = @TenantId ORDER BY UpdatedAt DESC";

        using var coreConnection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
        var coreSessions = await coreConnection.QueryAsync<ReceptionistAgent.Core.Models.ChatSessionDto>(coreSql, new { TenantId = tenantId });
        return coreSessions.Select(s => { if (_isCorePostgres) s.TenantId = tenantId; return s; }).ToList();
    }
}
