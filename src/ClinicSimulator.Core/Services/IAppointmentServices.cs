using ClinicSimulator.Core.Models;

namespace ClinicSimulator.Core.Services;

public interface IAppointmentService
{
    Task<List<TimeSlot>> GetAvailableSlotsAsync(string doctorId, DateTime date);
    Task<Appointment> BookAppointmentAsync(
        string patientName,
        string phone,
        string email,
        string doctorId,
        DateTime date,
        TimeSpan time,
        string reason);
    Task<bool> CancelAppointmentAsync(string confirmationCode);
    Task<Appointment?> GetAppointmentAsync(string confirmationCode);
    Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date);

    List<Doctor> GetAllDoctors();
    Doctor? GetDoctorByName(string name);
}