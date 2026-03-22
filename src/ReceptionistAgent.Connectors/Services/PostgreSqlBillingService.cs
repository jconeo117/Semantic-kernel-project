using Dapper;
using Npgsql;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;

namespace ReceptionistAgent.Connectors.Services;

public class PostgreSqlBillingService : IBillingService
{
    private readonly string _connectionString;

    public PostgreSqlBillingService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<TenantBilling?> GetBillingAsync(string tenantId)
    {
        const string sql = "SELECT tenant_id AS \"TenantId\", plan_type AS \"PlanType\", billing_status AS \"BillingStatus\", active_until AS \"ActiveUntil\", suspended_at AS \"SuspendedAt\", suspension_reason AS \"SuspensionReason\", notes AS \"Notes\" FROM tenant_billing WHERE tenant_id = @TenantId";
        using var connection = new NpgsqlConnection(_connectionString);
        var entity = await connection.QuerySingleOrDefaultAsync<BillingEntity>(sql, new { TenantId = tenantId });

        return entity == null ? null : MapToModel(entity);
    }

    public async Task<bool> IsTenantAllowedAsync(string tenantId)
    {
        var billing = await GetBillingAsync(tenantId);

        if (billing == null)
            return true;

        if (billing.BillingStatus != BillingStatus.Active)
            return false;

        if (billing.ActiveUntil.HasValue && billing.ActiveUntil.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    public async Task SuspendTenantAsync(string tenantId, string reason)
    {
        const string sql = @"
            UPDATE tenant_billing SET
                billing_status = @Status,
                suspended_at = @Now,
                suspension_reason = @Reason
            WHERE tenant_id = @TenantId";

        using var connection = new NpgsqlConnection(_connectionString);
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
            UPDATE tenant_billing SET
                billing_status = @Status,
                active_until = @ActiveUntil,
                suspended_at = NULL,
                suspension_reason = NULL
            WHERE tenant_id = @TenantId";

        using var connection = new NpgsqlConnection(_connectionString);
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
            UPDATE tenant_billing SET
                plan_type = @PlanType,
                billing_status = @BillingStatus,
                active_until = @ActiveUntil,
                suspended_at = @SuspendedAt,
                suspension_reason = @SuspensionReason,
                notes = @Notes
            WHERE tenant_id = @TenantId";

        using var connection = new NpgsqlConnection(_connectionString);
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
            INSERT INTO tenant_billing (tenant_id, plan_type, billing_status, active_until, notes)
            VALUES (@TenantId, @PlanType, @BillingStatus, @ActiveUntil, @Notes)";

        using var connection = new NpgsqlConnection(_connectionString);
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
