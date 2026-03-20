using Dapper;
using Npgsql;
using Microsoft.Data.SqlClient;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;
using ReceptionistAgent.Core.Utils;

namespace ReceptionistAgent.Connectors.Services;

/// <summary>
/// Servicio de recordatorios respaldado por PostgreSQL.
/// Maneja escritura primaria en Postgres y backup en SQL Server (AgentCore).
/// </summary>
public class PostgreSqlReminderService : IReminderService
{
    private readonly string _agentCoreConnectionString;
    private readonly string? _tenantConnectionString;

    public PostgreSqlReminderService(string agentCoreConnectionString, string? tenantConnectionString = null)
    {
        _agentCoreConnectionString = agentCoreConnectionString;
        _tenantConnectionString = tenantConnectionString;
        _isCorePostgres = agentCoreConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);
    }
    private readonly bool _isCorePostgres;

    public async Task ScheduleRemindersForBookingAsync(BookingRecord booking, string recipientPhone, string countryCode = "", string timeZoneId = "UTC")
    {
        var normalizedPhone = PhoneNormalizer.Normalize(recipientPhone, countryCode);

        DateTime appointmentDateTimeUtc;
        try
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localDateTime = booking.ScheduledDate.Date + booking.ScheduledTime;
            appointmentDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localDateTime, tzInfo);
        }
        catch
        {
            appointmentDateTimeUtc = booking.ScheduledDate.Date + booking.ScheduledTime;
        }

        var reminders = new List<Reminder>
        {
            new()
            {
                TenantId = booking.TenantId,
                BookingId = booking.Id,
                ReminderType = ReminderType.Before24h,
                ScheduledFor = appointmentDateTimeUtc.AddHours(-24),
                RecipientPhone = normalizedPhone,
                MessageContent = $"Recordatorio: Tiene una cita mañana {booking.ScheduledDate:yyyy-MM-dd} a las {booking.ScheduledTime:hh\\:mm} con {booking.ProviderName}. Código: {booking.ConfirmationCode}. Recuerde llegar 15 minutos antes."
            },
            new()
            {
                TenantId = booking.TenantId,
                BookingId = booking.Id,
                ReminderType = ReminderType.Before1h,
                ScheduledFor = appointmentDateTimeUtc.AddHours(-1),
                RecipientPhone = normalizedPhone,
                MessageContent = $"Recordatorio: Su cita es en 1 hora ({booking.ScheduledTime:hh\\:mm}) con {booking.ProviderName}. Código: {booking.ConfirmationCode}."
            }
        };

        var now = DateTime.UtcNow;
        var futureReminders = reminders.Where(r => r.ScheduledFor > now).ToList();

        if (!futureReminders.Any()) return;

        // 1. Primary Write (Tenant DB - PostgreSQL)
        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            try
            {
                const string tenantSql = @"
                    INSERT INTO reminders (id, booking_id, reminder_type, scheduled_for, status, channel, recipient_phone, message_content, created_at)
                    VALUES (@Id, @BookingId, @ReminderType, @ScheduledFor, @Status, @Channel, @RecipientPhone, @MessageContent, @CreatedAt)";

                using var connection = new NpgsqlConnection(_tenantConnectionString);
                foreach (var reminder in futureReminders)
                {
                    await connection.ExecuteAsync(tenantSql, new
                    {
                        reminder.Id,
                        reminder.BookingId,
                        ReminderType = reminder.ReminderType.ToString(),
                        reminder.ScheduledFor,
                        Status = reminder.Status.ToString(),
                        reminder.Channel,
                        reminder.RecipientPhone,
                        reminder.MessageContent,
                        reminder.CreatedAt
                    });
                }
            }
            catch { }
        }

        // 2. Backup Write (AgentCore) - Fire & Forget
        _ = Task.Run(async () =>
        {
            try
            {
                string coreSql = _isCorePostgres
                    ? "INSERT INTO reminders (id, tenant_id, booking_id, reminder_type, scheduled_for, status, channel, recipient_phone, message_content, created_at) VALUES (@Id, @TenantId, @BookingId, @ReminderType, @ScheduledFor, @Status, @Channel, @RecipientPhone, @MessageContent, @CreatedAt)"
                    : "INSERT INTO Reminders (Id, TenantId, BookingId, ReminderType, ScheduledFor, Status, Channel, RecipientPhone, MessageContent, CreatedAt) VALUES (@Id, @TenantId, @BookingId, @ReminderType, @ScheduledFor, @Status, @Channel, @RecipientPhone, @MessageContent, @CreatedAt)";

                using var coreConnection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
                foreach (var reminder in futureReminders)
                {
                    await coreConnection.ExecuteAsync(coreSql, new
                    {
                        reminder.Id,
                        reminder.TenantId,
                        reminder.BookingId,
                        ReminderType = reminder.ReminderType.ToString(),
                        reminder.ScheduledFor,
                        Status = reminder.Status.ToString(),
                        reminder.Channel,
                        reminder.RecipientPhone,
                        reminder.MessageContent,
                        reminder.CreatedAt
                    });
                }
            }
            catch { }
        });
    }

    public async Task<List<Reminder>> GetPendingRemindersAsync(DateTime before)
    {
        // For PostgreSQL, we only read from the tenant DB if configured, otherwise fallback to AgentCore (SQL Server)
        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            const string sql = @"
                SELECT id as Id, booking_id as BookingId, reminder_type as ReminderType, scheduled_for as ScheduledFor, 
                       status as Status, channel as Channel, recipient_phone as RecipientPhone, 
                       message_content as MessageContent, created_at as CreatedAt
                FROM reminders
                WHERE status = 'Pending' AND scheduled_for <= @Before
                ORDER BY scheduled_for";

            using var connection = new NpgsqlConnection(_tenantConnectionString);
            var entities = await connection.QueryAsync<ReminderEntity>(sql, new { Before = before });
            return entities.Select(MapToModel).ToList();
        }
        else
        {
            string coreSql = _isCorePostgres
                ? "SELECT id as Id, tenant_id as TenantId, booking_id as BookingId, reminder_type as ReminderType, scheduled_for as ScheduledFor, status as Status, channel as Channel, recipient_phone as RecipientPhone, message_content as MessageContent, created_at as CreatedAt FROM reminders WHERE status = 'Pending' AND scheduled_for <= @Before ORDER BY scheduled_for"
                : "SELECT * FROM Reminders WHERE Status = 'Pending' AND ScheduledFor <= @Before ORDER BY ScheduledFor";
            
            using var connection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
            var entities = await connection.QueryAsync<ReminderEntity>(coreSql, new { Before = before });
            return entities.Select(MapToModel).ToList();
        }
    }

    public async Task MarkAsSentAsync(Guid reminderId)
    {
        const string pgSql = "UPDATE reminders SET status = 'Sent', sent_at = @Now WHERE id = @Id";
        const string sqlServerSql = "UPDATE Reminders SET Status = 'Sent', SentAt = @Now WHERE Id = @Id";

        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            using var connection = new NpgsqlConnection(_tenantConnectionString);
            await connection.ExecuteAsync(pgSql, new { Id = reminderId, Now = DateTime.UtcNow });
        }

        _ = Task.Run(async () =>
        {
            try
            {
                string sql = _isCorePostgres ? pgSql : sqlServerSql;

                using var coreConnection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
                await coreConnection.ExecuteAsync(sql, new { Id = reminderId, Now = DateTime.UtcNow });
            }
            catch { }
        });
    }

    public async Task MarkAsFailedAsync(Guid reminderId, string error)
    {
        const string pgSql = "UPDATE reminders SET status = 'Failed', error_message = @Error WHERE id = @Id";
        const string sqlServerSql = "UPDATE Reminders SET Status = 'Failed', ErrorMessage = @Error WHERE Id = @Id";

        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            using var connection = new NpgsqlConnection(_tenantConnectionString);
            await connection.ExecuteAsync(pgSql, new { Id = reminderId, Error = error });
        }

        _ = Task.Run(async () =>
        {
            try
            {
                string sql = _isCorePostgres ? pgSql : sqlServerSql;

                using var coreConnection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
                await coreConnection.ExecuteAsync(sql, new { Id = reminderId, Error = error });
            }
            catch { }
        });
    }

    public async Task CancelRemindersForBookingAsync(Guid bookingId)
    {
        const string pgSql = "UPDATE reminders SET status = 'Cancelled' WHERE booking_id = @BookingId AND status = 'Pending'";
        const string sqlServerSql = "UPDATE Reminders SET Status = 'Cancelled' WHERE BookingId = @BookingId AND Status = 'Pending'";

        if (!string.IsNullOrEmpty(_tenantConnectionString))
        {
            using var connection = new NpgsqlConnection(_tenantConnectionString);
            await connection.ExecuteAsync(pgSql, new { BookingId = bookingId });
        }

        _ = Task.Run(async () =>
        {
            try
            {
                string sql = _isCorePostgres ? pgSql : sqlServerSql;

                using var coreConnection = _isCorePostgres ? (System.Data.IDbConnection)new NpgsqlConnection(_agentCoreConnectionString) : new SqlConnection(_agentCoreConnectionString);
                await coreConnection.ExecuteAsync(sql, new { BookingId = bookingId });
            }
            catch { }
        });
    }

    private static Reminder MapToModel(ReminderEntity entity)
    {
        Enum.TryParse<ReminderType>(entity.ReminderType, true, out var reminderType);
        Enum.TryParse<ReminderStatus>(entity.Status, true, out var status);

        return new Reminder
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            BookingId = entity.BookingId,
            ReminderType = reminderType,
            ScheduledFor = entity.ScheduledFor,
            Status = status,
            Channel = entity.Channel ?? "WhatsApp",
            RecipientPhone = entity.RecipientPhone ?? "",
            MessageContent = entity.MessageContent,
            SentAt = entity.SentAt,
            ErrorMessage = entity.ErrorMessage,
            CreatedAt = entity.CreatedAt
        };
    }

    private class ReminderEntity
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = "";
        public Guid BookingId { get; set; }
        public string ReminderType { get; set; } = "";
        public DateTime ScheduledFor { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Channel { get; set; }
        public string? RecipientPhone { get; set; }
        public string? MessageContent { get; set; }
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
