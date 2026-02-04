using System.ComponentModel;
using ClinicSimulator.Core.Services;
using Microsoft.SemanticKernel;
using ClinicSimulator.Core.Models;

namespace ClinicSimulator.AI.Plugins;
public class AppointmentPlugin
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentPlugin(IAppointmentService appointmentServices)
    {
        _appointmentService = appointmentServices;
    }

    [KernelFunction]
    [Description("DEPRECADO: Usa FindAvailableSlots en su lugar. Solo úsala si tienes el ID exacto del doctor.")]
    public async Task<string> GetAvalibleSlots(
        [Description("Id del doctor")] string doctorId,
        [Description("Fecha en formato YYYY-MM-DD")] string stringDate
    )
    {
        if (!DateTime.TryParse(stringDate, out var date))
            return "Por favor, usar el formato YYYY-MM-DD";

        var slots = await _appointmentService.GetAvailableSlotsAsync(doctorId, date);

        if (!slots.Any())
            return "No hay horarios disponibles para ese día, pro favor, pruebe con otra fecha";

        var availableSlots = slots.Where(s => s.IsAvailable).ToList();

        if (!availableSlots.Any())
            return "Todos los horarios estan ocupados para ese día, pro favor, pruebe con otra fecha";

        var times = availableSlots.Select(s => s.Time.ToString(@"hh\:mm")).ToList();
        return $"Horarios disponibles para {stringDate}: {string.Join(", ", times)}";
    }

    [KernelFunction]
    [Description("Busca horarios disponibles. Puede buscar por nombre de doctor, especialidad, o mostrar todos los doctores disponibles")]
    public async Task<string> FindAvailableSlots(
    [Description("Nombre del doctor (ej: 'Dr. Ramírez', 'Carlos'), especialidad (ej: 'oftalmología', 'retina'), o 'cualquiera' para ver todos")] string doctorQuery,
    [Description("Fecha en formato YYYY-MM-DD")] string stringDate
)
    {
        if (!DateTime.TryParse(stringDate, out var date))
            return "Por favor, usar el formato YYYY-MM-DD";

        // Obtener lista de doctores del servicio
        var allDoctors = _appointmentService.GetAllDoctors(); // ← Necesitamos agregar esto

        // Buscar doctores que coincidan
        var matchingDoctors = allDoctors.Where(d =>
            doctorQuery.ToLower() == "cualquiera" ||
            d.Name.Contains(doctorQuery, StringComparison.OrdinalIgnoreCase) ||
            d.Specialization.Contains(doctorQuery, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (!matchingDoctors.Any())
            return $"No se encontró doctor que coincida con '{doctorQuery}'. Doctores disponibles: {string.Join(", ", allDoctors.Select(d => d.Name))}";

        // Buscar slots para cada doctor
        var results = new List<string>();

        foreach (var doctor in matchingDoctors)
        {
            var slots = await _appointmentService.GetAvailableSlotsAsync(doctor.Id, date);
            var availableSlots = slots.Where(s => s.IsAvailable).ToList();

            if (availableSlots.Any())
            {
                var times = availableSlots.Select(s => s.Time.ToString(@"hh\:mm")).ToList();
                results.Add($"• {doctor.Name} ({doctor.Specialization}): {string.Join(", ", times)}");
            }
        }

        if (!results.Any())
            return $"No hay horarios disponibles para {date:yyyy-MM-dd}";

        return $"Horarios disponibles para {date:yyyy-MM-dd}:\n{string.Join("\n", results)}";
    }

    [KernelFunction]
    [Description("Agenda una nueva cita médica. Si faltan datos, devuelve un error descriptivo.")]
    public async Task<string> BookAppointment(
    [Description("Nombre completo del paciente")] string PatientName,
    [Description("Telefono celular del paciente")] string PatientPhone,
    [Description("Correo electronico del paciente (Opcional, usar 'no-email' si no se tiene)")] string PatientEmail,
    [Description("Nombre del doctor (ej: 'Dr. Ramírez') o ID (ej: 'DR001')")] string doctorNameOrId, // ← CAMBIAR
    [Description("Fecha para agendar la cita. Formato YYYY-MM-DD")] string stringDate,
    [Description("Horario para agendar la cita. Formato 24 horas HH:MM")] string stringtime,
    [Description("Razon de la cita")] string reason
)
    {
        if (string.IsNullOrEmpty(PatientEmail) || PatientEmail == "no-email")
        {
            return "FALLO: No se puede agendar. Falta el CORREO ELECTRÓNICO del paciente. Por favor pídeselo al usuario.";
        }

        try
        {
            if (!DateTime.TryParse(stringDate, out var date))
                return $"FALLO: La fecha '{stringDate}' no es válida. Usa formato YYYY-MM-DD.";

            if (!TimeSpan.TryParse(stringtime, out var time))
                return $"FALLO: La hora '{stringtime}' no es válida. Usa formato HH:MM (24h).";

            // ✅ BUSCAR DOCTOR POR NOMBRE O ID
            var allDoctors = _appointmentService.GetAllDoctors();
            var doctor = allDoctors.FirstOrDefault(d =>
                d.Id == doctorNameOrId ||
                d.Name.Contains(doctorNameOrId, StringComparison.OrdinalIgnoreCase)
            );

            if (doctor == null)
                return $"FALLO: No se encontró doctor '{doctorNameOrId}'. Doctores disponibles: {string.Join(", ", allDoctors.Select(d => d.Name))}";

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = PatientName,
                Phone = PatientPhone,
                Email = PatientEmail
            };

            var appointment = await _appointmentService.BookAppointmentAsync(
                patient.Name,
                patient.Phone,
                patient.Email,
                doctor.Id,  // ← Usar el ID encontrado
                date,
                time,
                reason
            );

            if (appointment != null)
            {
                return $"ÉXITO: Cita confirmada exitosamente. \n" +
                       $"Código de Confirmación: {appointment.ConfirmationId} \n" +
                       $"Paciente: {patient.Name} \n" +
                       $"Doctor: {doctor.Name} ({doctor.Specialization})\n" +
                       $"Fecha: {date:yyyy-MM-dd} a las {time} \n" +
                       $"INSTRUCCIÓN PARA EL AGENTE: Informa al usuario el código de confirmación y recuérdale llegar 15 minutos antes.";
            }

            return "FALLO: El horario seleccionado ya no está disponible.";
        }
        catch (Exception ex)
        {
            return $"ERROR DEL SISTEMA: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Busca la primera cita disponible con cualquier doctor desde hoy hacia adelante")]
    public async Task<string> GetFirstAvailableAppointment(
    [Description("Número de días hacia adelante a buscar (default: 30)")] int daysToSearch = 30
)
    {
        var today = DateTime.Now.Date;
        var allDoctors = _appointmentService.GetAllDoctors();

        for (int i = 0; i < daysToSearch; i++)
        {
            var date = today.AddDays(i);

            foreach (var doctor in allDoctors)
            {
                var slots = await _appointmentService.GetAvailableSlotsAsync(doctor.Id, date);
                var availableSlots = slots.Where(s => s.IsAvailable).ToList();

                if (availableSlots.Any())
                {
                    var times = availableSlots.Select(s => s.Time.ToString(@"hh\:mm")).Take(5).ToList();
                    return $"Primera cita disponible:\n" +
                           $"Doctor: {doctor.Name} ({doctor.Specialization})\n" +
                           $"Fecha: {date:yyyy-MM-dd} ({date:dddd})\n" +
                           $"Horarios: {string.Join(", ", times)}";
                }
            }
        }

        return $"No hay disponibilidad en los próximos {daysToSearch} días";
    }

    [KernelFunction]
    [Description("Cancelar una cita medica mediante codigo de confirmacion.")]
    public async Task<string> CancelAppointment(
        [Description("Codigo de confirmacion de la cita")] string confirmationCode
    )
    {
        var appointment = await _appointmentService.GetAppointmentAsync(confirmationCode);
        if (appointment == null)
            return $"La cita con el codigo {confirmationCode} no fue encontrada, pruebe nuevamente.";

        var success = await _appointmentService.CancelAppointmentAsync(confirmationCode);

        if (success)
            return $"✓ Cita cancelada: {appointment.PatientName}, " +
                   $"{appointment.AppointmentDate:yyyy-MM-dd} {appointment.AppointmentTime:hh\\:mm}";

        return "Error al cancelar la cita";
    }

    [KernelFunction]
    [Description("Obtener informacion de una cita medica")]
    public async Task<string> GetAppointmentInfo(
        [Description("Codigo de confirmacion de la cita")] string confirmationCode
    )
    {
        var appointment = await _appointmentService.GetAppointmentAsync(confirmationCode);
        if (appointment == null)
            return $"La cita con el codigo {confirmationCode} no fue encontrada, pruebe nuevamente.";

        return $"Cita {appointment.ConfirmationId}:\n" +
               $"Paciente: {appointment.PatientName}\n" +
               $"Doctor: {appointment.DoctorName}\n" +
               $"Fecha: {appointment.AppointmentDate:yyyy-MM-dd}\n" +
               $"Hora: {appointment.AppointmentTime:hh\\:mm}\n" +
               $"Motivo: {appointment.Reason}\n" +
               $"Estado: {appointment.Status}";
    }

    [KernelFunction]
    [Description("Lista todas las citas agendadas para el dia de hoy.")]
    public async Task<string> GetAllAppointmentsByDate()
    {
        var appointments = await _appointmentService.GetAppointmentsByDateAsync(DateTime.Now.Date);
        if (!appointments.Any())
            return "no hay citas agendadas para hoy";

        return string.Join("\n", appointments.Select(a =>
            $"[{a.ConfirmationId}] {a.PatientName} - {a.DoctorName} - " +
            $"{a.AppointmentDate:yyyy-MM-dd} {a.AppointmentTime:hh\\:mm}"));
    }
}