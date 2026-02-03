using ClinicSimulator.Core.Models;

namespace ClinicSimulator.Core.Repositories;
public interface IAppointmentRepository
{
    Task<Appointment> CreateAsync(Appointment appointment);
    Task<Appointment> GetByappointmentId(string id);
    Task<Appointment> GetByConfirmationCode(string confirmationCode);
    Task<List<Appointment>> GetbyDate(DateTime date);
    Task<List<Appointment>> GetbyDoctorId(string doctorId);
    Task<List<Appointment>> GetAllAsync();
    Task<bool> UpdateAsync(Appointment appointment);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(DateTime date, TimeSpan time, string doctorId);
}