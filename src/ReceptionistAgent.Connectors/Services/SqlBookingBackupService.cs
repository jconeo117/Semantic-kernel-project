using Dapper;
using Microsoft.Data.SqlClient;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;
using System.Text.Json;

namespace ReceptionistAgent.Connectors.Services;

public class SqlBookingBackupService : IBookingBackupService
{
    private readonly string _connectionString;

    public SqlBookingBackupService(string connectionString)
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
                    IF NOT EXISTS (SELECT 1 FROM Bookings WHERE Id = @Id AND TenantId = @TenantId)
                    BEGIN
                        INSERT INTO Bookings (Id, TenantId, ConfirmationCode, ClientName, ProviderId, ProviderName, ScheduledDate, ScheduledTime, Status, CreatedAt, UpdatedAt, CustomFieldsJson)
                        VALUES (@Id, @TenantId, @ConfirmationCode, @ClientName, @ProviderId, @ProviderName, @ScheduledDate, @ScheduledTime, @Status, @CreatedAt, @UpdatedAt, @CustomFieldsJson)
                    END
                    ELSE
                    BEGIN
                        UPDATE Bookings SET 
                            Status = @Status, 
                            UpdatedAt = @UpdatedAt, 
                            CustomFieldsJson = @CustomFieldsJson 
                        WHERE Id = @Id AND TenantId = @TenantId
                    END";

                using var connection = new SqlConnection(_connectionString);
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
                // Solo loggear en sistema real, aquí fallamos silenciosamente (fire & forget)
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
                const string sql = "UPDATE Bookings SET Status = @Status, UpdatedAt = @UpdatedAt WHERE Id = @Id AND TenantId = @TenantId";
                using var connection = new SqlConnection(_connectionString);
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
