using System.ComponentModel;
using ClinicSimulator.Core.Services;
using Microsoft.SemanticKernel;

namespace ClinicSimulator.AI.Plugins;
public class AppointmentPlugin
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentPlugin(IAppointmentService appointmentServices)
    {
        _appointmentService = appointmentServices;
    }

    [KernelFunction]
    [Description("Obtiene los horarios disponibles para un doctor en una fecha específica")]
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
    [Description("agenda una nueva cita medica. Si faltan datos, devuelve un error descriptivo.")]
    public async Task<string> BookAppointment(
        [Description("Nombre completo del paciente")] string PatientName,
        [Description("Telefono celular del paciente")] string PatientPhone,
        [Description("Correo electronico del paciente (Opcional, usar 'no-email' si no se tiene)")] string PatientEmail,
        [Description("Id del doctor")] string doctorId,
        [Description("Fecha para agendar la cita. Formato YYYY-MM-DD")] string stringDate,
        [Description("Horario para agendar la cita. Fomato 24 horas HH:MM")] string stringtime,
        [Description("Razon de la cita")] string reason
    )
    {
        // 1. Validación Manual (Human-in-the-loop simulado)
        // Esto ayuda al LLM a saber qué preguntar si faltan cosas.
        if (string.IsNullOrEmpty(PatientEmail) || PatientEmail == "no-email")
        {
            // Retorno instrucciones para el LLM
            return "FALLO: No se puede agendar. Falta el CORREO ELECTRÓNICO del paciente. Por favor pídeselo al usuario.";
        }

        try
        {
            // Parseo de fecha robusto
            if (!DateTime.TryParse(stringDate, out var date))
                return $"FALLO: La fecha '{stringDate}' no es válida. Usa formato YYYY-MM-DD.";

            if (!TimeSpan.TryParse(stringtime, out var time))
                return $"FALLO: La hora '{stringtime}' no es válida. Usa formato HH:MM (24h).";

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = PatientName,
                Phone = PatientPhone,
                Email = PatientEmail
            };

            var appointment = await _appointmentService.BookAppointmentAsync(patient.Name, patient.Phone, patient.Email, doctorId, date, time, reason);

            if (appointment != null)
            {
                // RETORNO RICO EN CONTEXTO:
                // Le damos al LLM todos los detalles para que pueda confirmar con seguridad.
                return $"ÉXITO: Cita confirmada exitosamente. \n" +
                       $"Código de Confirmación: {appointment.ConfirmationId} \n" +
                       $"Paciente: {patient.Name} \n" +
                       $"Fecha: {date} a las {time} \n" +
                       $"Doctor ID: {doctorId}. \n" +
                       $"INSTRUCCIÓN PARA EL AGENTE: Informa al usuario el código de confirmación y recuérdale llegar 15 minutos antes.";
            }

            return "FALLO: El horario seleccionado ya no está disponible o el doctor no atiende a esa hora.";
        }
        catch (Exception ex)
        {
            return $"ERROR DEL SISTEMA: {ex.Message}";
        }
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