using ClinicSimulator.Core.Models;

namespace ClinicSimulator.Core.Repositories;
public class InMemoryAppointments : IAppointmentRepository
{
    private List<Appointment> _appointments = new List<Appointment>();
    public Task<Appointment> CreateAsync(Appointment appointment)
    {
        appointment.Id = Guid.NewGuid();
        appointment.ConfirmationId = $"CITA - {appointment.Id:D4}";
        appointment.CreatedAt = DateTime.Now;
        _appointments.Add(appointment);
        return Task.FromResult(appointment);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var appointment = _appointments.FirstOrDefault(a => a.Id == Guid.Parse(id));
        if (appointment == null)
        {
            return Task.FromResult(false);
        }
        _appointments.Remove(appointment);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(DateTime date, TimeSpan time, string doctorId)
    {
        var appointment = _appointments.FirstOrDefault(a => a.AppointmentDate == date && a.AppointmentTime == time && a.DoctorId == doctorId && a.Status != AppointmentStatus.Cancelled);
        return Task.FromResult(appointment != null);
    }

    public Task<List<Appointment>> GetAllAsync()
    {
        return Task.FromResult(_appointments);
    }

    public Task<Appointment> GetByappointmentId(string id)
    {
        var appointment = _appointments.FirstOrDefault(a => a.Id == Guid.Parse(id));
        return Task.FromResult(appointment);
    }

    public Task<Appointment> GetByConfirmationCode(string confirmationCode)
    {
        var appointment = _appointments.FirstOrDefault(a => a.ConfirmationId == confirmationCode);
        return Task.FromResult(appointment);
    }

    public Task<List<Appointment>> GetbyDate(DateTime date)
    {
        var appointmentsByDate = _appointments.Where(a => a.AppointmentDate == date).ToList();
        return Task.FromResult(appointmentsByDate);
    }

    public Task<List<Appointment>> GetbyDoctorId(string doctorId)
    {
        var appointmentsByDoctor = _appointments.Where(a => a.DoctorId == doctorId).ToList();
        return Task.FromResult(appointmentsByDoctor);
    }

    public Task<bool> UpdateAsync(Appointment appointment)
    {
        var existing = _appointments.FirstOrDefault(a => a.Id == appointment.Id);
        if (existing == null) return Task.FromResult(false);

        var index = _appointments.IndexOf(existing);
        _appointments[index] = appointment;
        return Task.FromResult(true);
    }
}