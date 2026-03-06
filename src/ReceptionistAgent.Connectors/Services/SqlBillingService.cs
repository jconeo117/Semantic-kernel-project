using Dapper;
using Microsoft.Data.SqlClient;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;

namespace ReceptionistAgent.Connectors.Services;

/// <summary>
/// Servicio de billing basado en SQL Server.
/// Controla acceso por fecha de expiración (ActiveUntil) y estado (BillingStatus).
/// </summary>
public class SqlBillingService : IBillingService
{
    private readonly string _connectionString;

    public SqlBillingService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<TenantBilling?> GetBillingAsync(string tenantId)
    {
        const string sql = "SELECT * FROM TenantBilling WHERE TenantId = @TenantId";
        using var connection = new SqlConnection(_connectionString);
        var entity = await connection.QuerySingleOrDefaultAsync<BillingEntity>(sql, new { TenantId = tenantId });

        return entity == null ? null : MapToModel(entity);
    }

    public async Task<bool> IsTenantAllowedAsync(string tenantId)
    {
        var billing = await GetBillingAsync(tenantId);

        // Si no tiene registro de billing, se permite acceso (tenant nuevo)
        if (billing == null)
            return true;

        // Verificar estado activo
        if (billing.BillingStatus != BillingStatus.Active)
            return false;

        // Verificar que no haya expirado
        if (billing.ActiveUntil.HasValue && billing.ActiveUntil.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    public async Task SuspendTenantAsync(string tenantId, string reason)
    {
        const string sql = @"
            UPDATE TenantBilling SET
                BillingStatus = @Status,
                SuspendedAt = @Now,
                SuspensionReason = @Reason
            WHERE TenantId = @TenantId";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Status = BillingStatus.Suspended.ToString(),
            Now = DateTime.UtcNow,
            Reason = reason
        });
    }

    public async Task ReactivateTenantAsync(string tenantId, DateTime activeUntil)
    {
        const string sql = @"
            UPDATE TenantBilling SET
                BillingStatus = @Status,
                ActiveUntil = @ActiveUntil,
                SuspendedAt = NULL,
                SuspensionReason = NULL
            WHERE TenantId = @TenantId";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Status = BillingStatus.Active.ToString(),
            ActiveUntil = activeUntil
        });
    }

    public async Task UpdateBillingAsync(TenantBilling billing)
    {
        const string sql = @"
            UPDATE TenantBilling SET
                PlanType = @PlanType,
                BillingStatus = @BillingStatus,
                ActiveUntil = @ActiveUntil,
                SuspendedAt = @SuspendedAt,
                SuspensionReason = @SuspensionReason,
                Notes = @Notes
            WHERE TenantId = @TenantId";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            billing.TenantId,
            PlanType = billing.PlanType.ToString(),
            BillingStatus = billing.BillingStatus.ToString(),
            billing.ActiveUntil,
            billing.SuspendedAt,
            billing.SuspensionReason,
            billing.Notes
        });
    }

    public async Task CreateBillingAsync(TenantBilling billing)
    {
        const string sql = @"
            INSERT INTO TenantBilling (TenantId, PlanType, BillingStatus, ActiveUntil, Notes)
            VALUES (@TenantId, @PlanType, @BillingStatus, @ActiveUntil, @Notes)";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            billing.TenantId,
            PlanType = billing.PlanType.ToString(),
            BillingStatus = billing.BillingStatus.ToString(),
            billing.ActiveUntil,
            billing.Notes
        });
    }

    private static TenantBilling MapToModel(BillingEntity entity)
    {
        Enum.TryParse<PlanType>(entity.PlanType, true, out var planType);
        Enum.TryParse<BillingStatus>(entity.BillingStatus, true, out var billingStatus);

        return new TenantBilling
        {
            TenantId = entity.TenantId,
            PlanType = planType,
            BillingStatus = billingStatus,
            ActiveUntil = entity.ActiveUntil,
            SuspendedAt = entity.SuspendedAt,
            SuspensionReason = entity.SuspensionReason,
            Notes = entity.Notes
        };
    }

    private class BillingEntity
    {
        public string TenantId { get; set; } = "";
        public string PlanType { get; set; } = "Trial";
        public string BillingStatus { get; set; } = "Active";
        public DateTime? ActiveUntil { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public string? Notes { get; set; }
    }
}
