using Dapper;
using Npgsql;
using ReceptionistAgent.Core.Models;
using System.Text.Json;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Services;
using Microsoft.Extensions.Logging;

namespace ReceptionistAgent.Connectors.Adapters;

/// <summary>
/// Implementación de IClientDataAdapter usando Dapper para PostgreSQL.
/// </summary>
public class PostgreSqlClientDataAdapter : IClientDataAdapter
{
    private readonly string _connectionString;
    private readonly IBookingBackupService _backupService;
    private readonly ILogger<PostgreSqlClientDataAdapter> _logger;

    public PostgreSqlClientDataAdapter(string connectionString, IBookingBackupService backupService, ILogger<PostgreSqlClientDataAdapter> logger)
    {
        _connectionString = connectionString;
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<List<ServiceProvider>> GetAllProvidersAsync()
    {
        const string sql = "SELECT * FROM providers WHERE is_active = true";

        using var connection = new NpgsqlConnection(_connectionString);
        var entities = await connection.QueryAsync<ProviderEntity>(sql);

        var providers = entities.Select(MapToProvider).ToList();
        _logger.LogInformation("Loaded {Count} providers from the PostgreSQL tenant database.", providers.Count);

        return providers;
    }

    public async Task<List<ServiceProvider>> SearchProvidersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ServiceProvider>();

        var providers = await GetAllProvidersAsync();
        var normalizedQuery = ReceptionistAgent.Core.Utils.TextHelper.RemoveAccents(query);

        return providers
            .Where(p => 
                ReceptionistAgent.Core.Utils.TextHelper.RemoveAccents(p.Name).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                ReceptionistAgent.Core.Utils.TextHelper.RemoveAccents(p.Role).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<BookingRecord> CreateBookingAsync(BookingRecord booking)
    {
        booking.Id = Guid.NewGuid();
        booking.ConfirmationCode = $"CITA-{booking.Id.ToString()[..4].ToUpper()}";
        booking.CreatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO bookings (
                id, tenant_id, confirmation_code, client_name, provider_id, provider_name, 
                scheduled_date, scheduled_time, status, created_at, custom_fields_json
            ) VALUES (
                @Id, @TenantId, @ConfirmationCode, @ClientName, @ProviderId, @ProviderName, 
                @ScheduledDate, @ScheduledTime, @Status, @CreatedAt, @CustomFieldsJson::jsonb
            )";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            booking.Id,
            booking.TenantId,
            booking.ConfirmationCode,
            booking.ClientName,
            booking.ProviderId,
            booking.ProviderName,
            booking.ScheduledDate,
            ScheduledTime = booking.ScheduledTime,
            Status = booking.Status.ToString(),
            booking.CreatedAt,
            CustomFieldsJson = JsonSerializer.Serialize(booking.CustomFields)
        });

        await _backupService.BackupAsync(booking, booking.TenantId);

        return booking;
    }

    public async Task<BookingRecord?> GetBookingByCodeAsync(string confirmationCode)
    {
        const string sql = "SELECT * FROM bookings WHERE confirmation_code = @Code";

        using var connection = new NpgsqlConnection(_connectionString);
        var entity = await connection.QuerySingleOrDefaultAsync<BookingEntity>(sql, new { Code = confirmationCode });

        return entity == null ? null : MapToRecord(entity);
    }

    public async Task<List<BookingRecord>> GetBookingsByDateAsync(DateTime date)
    {
        const string sql = "SELECT * FROM bookings WHERE scheduled_date = @Date AND status != @CancelledStatus";

        using var connection = new NpgsqlConnection(_connectionString);
        var entities = await connection.QueryAsync<BookingEntity>(sql, new
        {
            Date = date.Date,
            CancelledStatus = BookingStatus.Cancelled.ToString()
        });

        return entities.Select(MapToRecord).ToList();
    }

    public async Task<List<BookingRecord>> GetAllBookingsAsync()
    {
        const string sql = "SELECT * FROM bookings";

        using var connection = new NpgsqlConnection(_connectionString);
        var entities = await connection.QueryAsync<BookingEntity>(sql);

        return entities.Select(MapToRecord).ToList();
    }

    public async Task<bool> UpdateBookingAsync(BookingRecord booking)
    {
        booking.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE bookings SET 
                client_name = @ClientName,
                provider_id = @ProviderId,
                provider_name = @ProviderName,
                scheduled_date = @ScheduledDate,
                scheduled_time = @ScheduledTime,
                status = @Status,
                updated_at = @UpdatedAt,
                custom_fields_json = @CustomFieldsJson::jsonb
            WHERE id = @Id";

        using var connection = new NpgsqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(sql, new
        {
            booking.ClientName,
            booking.ProviderId,
            booking.ProviderName,
            booking.ScheduledDate,
            ScheduledTime = booking.ScheduledTime,
            Status = booking.Status.ToString(),
            booking.UpdatedAt,
            CustomFieldsJson = JsonSerializer.Serialize(booking.CustomFields),
            booking.Id
        });

        if (affected > 0)
        {
            await _backupService.UpdateStatusBackupAsync(booking.Id, booking.TenantId, booking.Status);
        }

        return affected > 0;
    }

    public async Task<bool> DeleteBookingAsync(string id)
    {
        if (!Guid.TryParse(id, out var bookingId)) return false;

        const string sql = "DELETE FROM bookings WHERE id = @Id";

        using var connection = new NpgsqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(sql, new { Id = bookingId });

        return affected > 0;
    }

    public async Task<bool> ExistsAsync(DateTime date, TimeSpan time, string providerId)
    {
        const string sql = @"
            SELECT COUNT(1) FROM bookings 
            WHERE provider_id = @ProviderId 
              AND scheduled_date = @Date 
              AND scheduled_time = @Time 
              AND status != @CancelledStatus";

        using var connection = new NpgsqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            ProviderId = providerId,
            Date = date.Date,
            Time = time,
            CancelledStatus = BookingStatus.Cancelled.ToString()
        });

        return count > 0;
    }

    public async Task<BookingRecord?> GetBookingByClientIdAsync(string clientId)
    {
        const string sql = "SELECT * FROM bookings WHERE custom_fields_json ->> 'clientId' = @ClientId AND status != @CancelledStatus ORDER BY scheduled_date DESC LIMIT 1";

        using var connection = new NpgsqlConnection(_connectionString);
        var entity = await connection.QueryFirstOrDefaultAsync<BookingEntity>(sql, new
        {
            ClientId = clientId,
            CancelledStatus = BookingStatus.Cancelled.ToString()
        });

        return entity == null ? null : MapToRecord(entity);
    }

    public async Task<List<BookingRecord>> GetBookingsByClientIdAsync(string clientId)
    {
        const string sql = "SELECT * FROM bookings WHERE custom_fields_json ->> 'clientId' = @ClientId ORDER BY scheduled_date DESC";

        using var connection = new NpgsqlConnection(_connectionString);
        var entities = await connection.QueryAsync<BookingEntity>(sql, new { ClientId = clientId });

        return entities.Select(MapToRecord).ToList();
    }

    private static BookingRecord MapToRecord(BookingEntity entity)
    {
        return new BookingRecord
        {
            Id = entity.id,
            TenantId = entity.tenant_id,
            ConfirmationCode = entity.confirmation_code,
            ClientName = entity.client_name,
            ProviderId = entity.provider_id,
            ProviderName = entity.provider_name,
            ScheduledDate = entity.scheduled_date,
            ScheduledTime = entity.scheduled_time.ToTimeSpan(),
            Status = Enum.TryParse<BookingStatus>(entity.status, true, out var status) ? status : BookingStatus.Scheduled,
            CreatedAt = entity.created_at,
            UpdatedAt = entity.updated_at,
            CustomFields = string.IsNullOrWhiteSpace(entity.custom_fields_json)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(entity.custom_fields_json) ?? new Dictionary<string, object>()
        };
    }

    private static ServiceProvider MapToProvider(ProviderEntity entity)
    {
        return new ServiceProvider
        {
            Id = entity.id,
            Name = entity.name,
            Role = entity.role,
            WorkingDays = ParseWorkingDays(entity.working_days),
            StartTime = entity.start_time.ToTimeSpan(),
            EndTime = entity.end_time.ToTimeSpan(),
            SlotDurationMinutes = entity.slot_duration_min,
            IsAvailable = entity.is_active
        };
    }

    private static List<DayOfWeek> ParseWorkingDays(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<DayOfWeek>();
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            return JsonSerializer.Deserialize<List<DayOfWeek>>(json, options) ?? new List<DayOfWeek>();
        }
        catch
        {
            try
            {
                var strings = JsonSerializer.Deserialize<List<string>>(json);
                if (strings == null) return new List<DayOfWeek>();
                return strings
                    .Select(s => Enum.TryParse<DayOfWeek>(s, true, out var day) ? day : (DayOfWeek?)null)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .ToList();
            }
            catch { return new List<DayOfWeek>(); }
        }
    }

    private class BookingEntity
    {
        public Guid id { get; set; }
        public string tenant_id { get; set; } = string.Empty;
        public string confirmation_code { get; set; } = string.Empty;
        public string client_name { get; set; } = string.Empty;
        public string provider_id { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public DateTime scheduled_date { get; set; }
        public TimeOnly scheduled_time { get; set; }
        public string status { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public string custom_fields_json { get; set; } = string.Empty;
    }

    private class ProviderEntity
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string? working_days { get; set; }
        public TimeOnly start_time { get; set; }
        public TimeOnly end_time { get; set; }
        public int slot_duration_min { get; set; }
        public bool is_active { get; set; }
    }
}
