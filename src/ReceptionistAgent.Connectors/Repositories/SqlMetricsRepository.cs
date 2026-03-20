using Dapper;
using Microsoft.Data.SqlClient;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Repositories;

namespace ReceptionistAgent.Connectors.Repositories;

public class SqlMetricsRepository : IMetricsRepository
{
    private readonly string _connectionString;

    public SqlMetricsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<MetricsSummary> GetMetricsAsync(string? tenantId, DateTime from, DateTime to)
    {
        using var connection = new SqlConnection(_connectionString);

        // Mensajes totales
        var messagesSql = tenantId != null
            ? "SELECT COUNT(*) FROM AuditLog WHERE TenantId = @TenantId AND Timestamp BETWEEN @From AND @To AND EventType LIKE '%UserMessage'"
            : "SELECT COUNT(*) FROM AuditLog WHERE Timestamp BETWEEN @From AND @To AND EventType LIKE '%UserMessage'";
        var totalMessages = await connection.QuerySingleAsync<int>(messagesSql, new { TenantId = tenantId, From = from, To = to });

        // Security blocks
        var blocksSql = tenantId != null
            ? "SELECT COUNT(*) FROM AuditLog WHERE TenantId = @TenantId AND Timestamp BETWEEN @From AND @To AND EventType = 'SecurityBlock'"
            : "SELECT COUNT(*) FROM AuditLog WHERE Timestamp BETWEEN @From AND @To AND EventType = 'SecurityBlock'";
        var securityBlocks = await connection.QuerySingleAsync<int>(blocksSql, new { TenantId = tenantId, From = from, To = to });

        // Mensajes por día
        var perDaySql = tenantId != null
            ? @"SELECT CAST(Timestamp AS DATE) as [Date], COUNT(*) as [Count]
                FROM AuditLog WHERE TenantId = @TenantId AND Timestamp BETWEEN @From AND @To AND EventType LIKE '%UserMessage'
                GROUP BY CAST(Timestamp AS DATE) ORDER BY [Date]"
            : @"SELECT CAST(Timestamp AS DATE) as [Date], COUNT(*) as [Count]
                FROM AuditLog WHERE Timestamp BETWEEN @From AND @To AND EventType LIKE '%UserMessage'
                GROUP BY CAST(Timestamp AS DATE) ORDER BY [Date]";
        var messagesPerDay = (await connection.QueryAsync<DailyCount>(perDaySql, new { TenantId = tenantId, From = from, To = to })).ToList();

        // Sessions únicas
        var sessionsSql = tenantId != null
            ? "SELECT COUNT(DISTINCT SessionId) FROM AuditLog WHERE TenantId = @TenantId AND Timestamp BETWEEN @From AND @To"
            : "SELECT COUNT(DISTINCT SessionId) FROM AuditLog WHERE Timestamp BETWEEN @From AND @To";
        var uniqueSessions = await connection.QuerySingleAsync<int>(sessionsSql, new { TenantId = tenantId, From = from, To = to });

        return new MetricsSummary
        {
            TenantId = tenantId ?? "all",
            From = from,
            To = to,
            TotalMessages = totalMessages,
            SecurityBlocks = securityBlocks,
            UniqueSessions = uniqueSessions,
            MessagesPerDay = messagesPerDay
        };
    }
}
