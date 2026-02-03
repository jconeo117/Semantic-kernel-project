public class TimeSlot
{
    // Fecha y hora
    public DateTime Date { get; set; }                    // DateTime - solo fecha
    public TimeSpan Time { get; set; }                    // TimeSpan - hora

    // Estado
    public bool IsAvailable { get; set; }                 // bool

    // Referencia (si está ocupado)
    public int? AppointmentId { get; set; }               // int? - nullable FK
}