using Dapper;
using Npgsql;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Repositories;

namespace ReceptionistAgent.Connectors.Repositories;

public class PostgreSqlMetricsRepository : IMetricsRepository
{
    private readonly string _connectionString;

    public PostgreSqlMetricsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ReceptionistAgent.Core.Models.MetricsSummary> GetMetricsAsync(string? tenantId, DateTime from, DateTime to)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        // Mensajes totales
        var messagesSql = tenantId != null
            ? "SELECT COUNT(*) FROM audits WHERE tenant_id = @TenantId AND timestamp BETWEEN @From AND @To AND event_type LIKE '%UserMessage'"
            : "SELECT COUNT(*) FROM audits WHERE timestamp BETWEEN @From AND @To AND event_type LIKE '%UserMessage'";
        var totalMessages = await connection.QuerySingleAsync<int>(messagesSql, new { TenantId = tenantId, From = from, To = to });

        // Security blocks
        var blocksSql = tenantId != null
            ? "SELECT COUNT(*) FROM audits WHERE tenant_id = @TenantId AND timestamp BETWEEN @From AND @To AND event_type = 'SecurityBlock'"
            : "SELECT COUNT(*) FROM audits WHERE timestamp BETWEEN @From AND @To AND event_type = 'SecurityBlock'";
        var securityBlocks = await connection.QuerySingleAsync<int>(blocksSql, new { TenantId = tenantId, From = from, To = to });

        // Mensajes por día
        var perDaySql = tenantId != null
            ? @"SELECT CAST(timestamp AS DATE) as ""Date"", COUNT(*) as ""Count""
                FROM audits WHERE tenant_id = @TenantId AND timestamp BETWEEN @From AND @To AND event_type LIKE '%UserMessage'
                GROUP BY CAST(timestamp AS DATE) ORDER BY ""Date"""
            : @"SELECT CAST(timestamp AS DATE) as ""Date"", COUNT(*) as ""Count""
                FROM audits WHERE timestamp BETWEEN @From AND @To AND event_type LIKE '%UserMessage'
                GROUP BY CAST(timestamp AS DATE) ORDER BY ""Date""";
        var messagesPerDay = (await connection.QueryAsync<DailyCount>(perDaySql, new { TenantId = tenantId, From = from, To = to })).ToList();

        // Sessions únicas
        var sessionsSql = tenantId != null
            ? "SELECT COUNT(DISTINCT session_id) FROM audits WHERE tenant_id = @TenantId AND timestamp BETWEEN @From AND @To"
            : "SELECT COUNT(DISTINCT session_id) FROM audits WHERE timestamp BETWEEN @From AND @To";
        var uniqueSessions = await connection.QuerySingleAsync<int>(sessionsSql, new { TenantId = tenantId, From = from, To = to });

        return new ReceptionistAgent.Core.Models.MetricsSummary
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

