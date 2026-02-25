namespace ClinicSimulator.Core.Models;

public class TimeSlot
{
    // Fecha y hora
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }

    // Estado
    public bool IsAvailable { get; set; }

    // Referencia (si está ocupado)
    public Guid? BookingId { get; set; }
}