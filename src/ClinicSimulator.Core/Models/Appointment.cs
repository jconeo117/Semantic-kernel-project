namespace ClinicSimulator.Core.Models;
public class Appointment
{

    //Id de la cita y codigo de confirmacion
    public Guid Id { get; set; }
    public string ConfirmationId { get; set; } = string.Empty;


    //informacion del paciente
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;

    //informacion del medico
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    //fecha y hora de la cita
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }


    //informacion de la cita
    public string Reason { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    //metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum AppointmentStatus
{
    Scheduled = 0,    // Agendada
    Confirmed = 1,    // Confirmada
    Cancelled = 2,    // Cancelada
    Completed = 3,    // Completada
    NoShow = 4        // No se presentó
}