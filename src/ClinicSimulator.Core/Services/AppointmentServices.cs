using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Services;
using ClinicSimulator.Core.Repositories;

namespace ClinicSimulator.Core.Services;
public class AppointmentServices : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly List<Doctor> _doctors;
    public AppointmentServices(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
        _doctors =
        [
            new() {
                Id = "DR0001",
                Name = "Dr. Carlos Ramírez",
                Specialization = "Oftalmología General",
                workingDays =
                [
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                ],
                startTime = new TimeSpan(9, 0, 0),
                endTime = new TimeSpan(18, 0, 0)
            },
            new() {
                Id = "DR0002",
                Name = "Dra. María González",
                Specialization = "Retina",
                workingDays =
                [
                    DayOfWeek.Monday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Friday
                ],
                startTime = new TimeSpan(10, 0, 0),
                endTime = new TimeSpan(16, 0, 0)
            }
        ];
    }

    public async Task<List<TimeSlot>> GetAvailableSlotsAsync(string doctorId, DateTime date)
    {
        var doctor = _doctors.FirstOrDefault(d => d.Id.ToString() == doctorId) ?? throw new ArgumentException("Doctor no encontrado");
        if (!doctor.workingDays.Contains(date.DayOfWeek))
            return [];

        var slots = new List<TimeSlot>();
        var currentTime = doctor.startTime;
        var slotDuration = TimeSpan.FromMinutes(30);

        while (currentTime < doctor.endTime)
        {
            var slot = new TimeSlot
            {
                Date = date,
                Time = currentTime,
                IsAvailable = !await _appointmentRepository.ExistsAsync(date, currentTime, doctorId)
            };

            slots.Add(slot);
            currentTime = currentTime.Add(slotDuration);
        }

        return slots;
    }

    public async Task<Appointment> BookAppointmentAsync(
        string patientName,
        string phone,
        string email,
        string doctorId,
        DateTime date,
        TimeSpan time,
        string reason)
    {
        if (await _appointmentRepository.ExistsAsync(date, time, doctorId))
        {
            throw new InvalidOperationException("El horario ya esta ocupado");
        }

        var doctor = _doctors.FirstOrDefault(d => d.Id.ToString() == doctorId) ?? throw new Exception("Doctor no encontrado");
        if (!doctor.workingDays.Contains(date.DayOfWeek))
        {
            throw new InvalidOperationException("El doctor no trabaja en esa fecha");
        }

        if (time < doctor.startTime || time > doctor.endTime)
        {
            throw new InvalidOperationException("El horario esta fuera del rango del doctor");
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientName = patientName,
            PatientPhone = phone,
            PatientEmail = email,
            DoctorId = doctorId,
            AppointmentDate = date,
            AppointmentTime = time,
            Reason = reason,
            Status = AppointmentStatus.Confirmed,
            CreatedAt = DateTime.Now
        };

        return await _appointmentRepository.CreateAsync(appointment);
    }

    public async Task<bool> CancelAppointmentAsync(string confirmationCode)
    {
        var appointment = await _appointmentRepository.GetByConfirmationCode(confirmationCode);
        if (appointment == null) return false;
        appointment.Status = AppointmentStatus.Cancelled;
        return await _appointmentRepository.UpdateAsync(appointment);
    }

    public async Task<Appointment?> GetAppointmentAsync(string confirmationCode)
    {
        return await _appointmentRepository.GetByConfirmationCode(confirmationCode);
    }

    public async Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date)
    {
        return await _appointmentRepository.GetbyDate(date);
    }

    public List<Doctor> GetAllDoctors()
    {
        return _doctors.ToList();
    }

    public Doctor? GetDoctorByName(string name)
    {
        return _doctors.FirstOrDefault(d =>
            d.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}