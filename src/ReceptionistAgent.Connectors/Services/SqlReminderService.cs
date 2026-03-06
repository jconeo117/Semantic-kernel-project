using Dapper;
using Microsoft.Data.SqlClient;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;

namespace ReceptionistAgent.Connectors.Services;

/// <summary>
/// Servicio de recordatorios respaldado por SQL Server.
/// Agenda recordatorios a 24h y 1-2h antes de la cita.
/// </summary>
public class SqlReminderService : IReminderService
{
    private readonly string _connectionString;

    public SqlReminderService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task ScheduleRemindersForBookingAsync(BookingRecord booking, string recipientPhone)
    {
        var appointmentDateTimeUtc = booking.ScheduledDate.Date + booking.ScheduledTime;

        var reminders = new List<Reminder>
        {
            // Recordatorio 24h antes
            new()
            {
                TenantId = booking.TenantId,
                BookingId = booking.Id,
                ReminderType = ReminderType.Before24h,
                ScheduledFor = appointmentDateTimeUtc.AddHours(-24),
                RecipientPhone = recipientPhone,
                MessageContent = $"Recordatorio: Tiene una cita mañana {booking.ScheduledDate:yyyy-MM-dd} a las {booking.ScheduledTime:hh\\:mm} con {booking.ProviderName}. Código: {booking.ConfirmationCode}. Recuerde llegar 15 minutos antes."
            },
            // Recordatorio 1h antes
            new()
            {
                TenantId = booking.TenantId,
                BookingId = booking.Id,
                ReminderType = ReminderType.Before1h,
                ScheduledFor = appointmentDateTimeUtc.AddHours(-1),
                RecipientPhone = recipientPhone,
                MessageContent = $"Recordatorio: Su cita es en 1 hora ({booking.ScheduledTime:hh\\:mm}) con {booking.ProviderName}. Código: {booking.ConfirmationCode}."
            }
        };

        // Solo agendar reminders futuros
        var now = DateTime.UtcNow;
        var futureReminders = reminders.Where(r => r.ScheduledFor > now).ToList();

        if (!futureReminders.Any()) return;

        const string sql = @"
            INSERT INTO Reminders (Id, TenantId, BookingId, ReminderType, ScheduledFor, Status, Channel, RecipientPhone, MessageContent, CreatedAt)
            VALUES (@Id, @TenantId, @BookingId, @ReminderType, @ScheduledFor, @Status, @Channel, @RecipientPhone, @MessageContent, @CreatedAt)";

        using var connection = new SqlConnection(_connectionString);
        foreach (var reminder in futureReminders)
        {
            await connection.ExecuteAsync(sql, new
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

    public async Task<List<Reminder>> GetPendingRemindersAsync(DateTime before)
    {
        const string sql = @"
            SELECT * FROM Reminders
            WHERE Status = 'Pending' AND ScheduledFor <= @Before
            ORDER BY ScheduledFor";

        using var connection = new SqlConnection(_connectionString);
        var entities = await connection.QueryAsync<ReminderEntity>(sql, new { Before = before });

        return entities.Select(MapToModel).ToList();
    }

    public async Task MarkAsSentAsync(Guid reminderId)
    {
        const string sql = "UPDATE Reminders SET Status = 'Sent', SentAt = @Now WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = reminderId, Now = DateTime.UtcNow });
    }

    public async Task MarkAsFailedAsync(Guid reminderId, string error)
    {
        const string sql = "UPDATE Reminders SET Status = 'Failed', ErrorMessage = @Error WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = reminderId, Error = error });
    }

    public async Task CancelRemindersForBookingAsync(Guid bookingId)
    {
        const string sql = "UPDATE Reminders SET Status = 'Cancelled' WHERE BookingId = @BookingId AND Status = 'Pending'";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { BookingId = bookingId });
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
