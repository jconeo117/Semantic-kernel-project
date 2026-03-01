using Dapper;
using Microsoft.Data.SqlClient;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Connectors.Security;
using System.Text.Json;

namespace ReceptionistAgent.Connectors.Security;

public class SqlAuditLogger : IAuditLogger
{
    private readonly string _connectionString;

    public SqlAuditLogger(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task LogAsync(AuditEntry entry)
    {
        entry.Timestamp = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO Audits (
                Id, TenantId, SessionId, Timestamp, EventType, Content, ThreatLevel, Metadata
            ) VALUES (
                @Id, @TenantId, @SessionId, @Timestamp, @EventType, @Content, @ThreatLevel, @Metadata
            )";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            entry.Id,
            entry.TenantId,
            entry.SessionId,
            entry.Timestamp,
            entry.EventType,
            entry.Content,
            ThreatLevel = entry.ThreatLevel?.ToString(),
            Metadata = JsonSerializer.Serialize(entry.Metadata)
        });
    }

    public async Task<List<AuditEntry>> GetSessionAuditAsync(Guid sessionId)
    {
        const string sql = "SELECT * FROM Audits WHERE SessionId = @SessionId ORDER BY Timestamp ASC";

        using var connection = new SqlConnection(_connectionString);
        var entries = await connection.QueryAsync<AuditEntry>(sql, new { SessionId = sessionId });

        return entries.ToList();
    }

    public async Task<List<AuditEntry>> GetSecurityEventsAsync(string? tenantId, DateTime from, DateTime to)
    {
        var sql = @"
            SELECT * FROM Audits 
            WHERE EventType IN ('SecurityBlock', 'OutputFiltered') 
              AND Timestamp >= @From AND Timestamp <= @To";

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            sql += " AND TenantId = @TenantId";
        }

        sql += " ORDER BY Timestamp DESC";

        using var connection = new SqlConnection(_connectionString);
        var entries = await connection.QueryAsync<AuditEntry>(sql, new
        {
            From = from,
            To = to,
            TenantId = tenantId
        });

        return entries.ToList();
    }

    public async Task<List<AuditEntry>> GetAllEventsAsync(string? tenantId, int limit = 100)
    {
        var topClause = limit > 0 ? $"TOP {limit} " : "";
        var sql = $"SELECT {topClause}* FROM Audits";

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            sql += " WHERE TenantId = @TenantId";
        }

        sql += " ORDER BY Timestamp DESC";

        using var connection = new SqlConnection(_connectionString);
        var entries = await connection.QueryAsync<AuditEntry>(sql, new { TenantId = tenantId });

        return entries.ToList();
    }
}
