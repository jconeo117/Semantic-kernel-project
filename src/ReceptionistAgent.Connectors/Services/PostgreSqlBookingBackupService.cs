using Dapper;
using Npgsql;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;
using System.Text.Json;

namespace ReceptionistAgent.Connectors.Services;

public class PostgreSqlBookingBackupService : IBookingBackupService
{
    private readonly string _connectionString;

    public PostgreSqlBookingBackupService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task BackupAsync(BookingRecord booking, string tenantId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                const string sql = @"
                    INSERT INTO bookings (id, tenant_id, confirmation_code, client_name, provider_id, provider_name, scheduled_date, scheduled_time, status, created_at, updated_at, custom_fields_json)
                    VALUES (@Id, @TenantId, @ConfirmationCode, @ClientName, @ProviderId, @ProviderName, @ScheduledDate, @ScheduledTime, @Status, @CreatedAt, @UpdatedAt, CAST(@CustomFieldsJson AS jsonb))
                    ON CONFLICT (id, tenant_id) DO UPDATE SET 
                        status = EXCLUDED.status, 
                        updated_at = EXCLUDED.updated_at, 
                        custom_fields_json = EXCLUDED.custom_fields_json";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    booking.Id,
                    TenantId = tenantId,
                    booking.ConfirmationCode,
                    booking.ClientName,
                    booking.ProviderId,
                    booking.ProviderName,
                    booking.ScheduledDate,
                    booking.ScheduledTime,
                    Status = booking.Status.ToString(),
                    booking.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    CustomFieldsJson = JsonSerializer.Serialize(booking.CustomFields)
                });
            }
            catch
            {
                // Silently fail in backup
            }
        });
        await Task.CompletedTask;
    }

    public async Task UpdateStatusBackupAsync(Guid bookingId, string tenantId, BookingStatus status)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                const string sql = "UPDATE bookings SET status = @Status, updated_at = @UpdatedAt WHERE id = @Id AND tenant_id = @TenantId";
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    Id = bookingId,
                    TenantId = tenantId,
                    Status = status.ToString(),
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch { }
        });
        await Task.CompletedTask;
    }
}
