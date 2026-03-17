using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Core.Services;

/// <summary>
/// Interfaz para el respaldo de citas en la base de datos central (AgentCore).
/// </summary>
public interface IBookingBackupService
{
    /// <summary>
    /// Realiza un respaldo asíncrono de una cita.
    /// </summary>
    Task BackupAsync(BookingRecord booking, string tenantId);

    /// <summary>
    /// Actualiza el estado de una cita en el respaldo.
    /// </summary>
    Task UpdateStatusBackupAsync(Guid bookingId, string tenantId, BookingStatus status);
}
