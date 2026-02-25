using System.ComponentModel;
using ClinicSimulator.Core.Services;
using ClinicSimulator.Core.Models;
using Microsoft.SemanticKernel;

namespace ClinicSimulator.AI.Plugins;

public class BookingPlugin
{
    private readonly IBookingService _bookingService;

    public BookingPlugin(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [KernelFunction]
    [Description("Busca horarios disponibles. Puede buscar por nombre de proveedor, rol/especialidad, o mostrar todos los disponibles")]
    public async Task<string> FindAvailableSlots(
        [Description("Nombre del proveedor (ej: 'Ramírez', 'Carlos'), rol (ej: 'oftalmología', 'retina'), o 'cualquiera' para ver todos")] string providerQuery,
        [Description("Fecha en formato YYYY-MM-DD")] string stringDate)
    {
        if (!DateTime.TryParse(stringDate, out var date))
            return "Por favor, usar el formato YYYY-MM-DD";

        List<ServiceProvider> matchingProviders;

        if (providerQuery.Equals("cualquiera", StringComparison.OrdinalIgnoreCase))
        {
            matchingProviders = await _bookingService.GetAllProvidersAsync();
        }
        else
        {
            matchingProviders = await _bookingService.SearchProvidersAsync(providerQuery);
        }

        if (!matchingProviders.Any())
        {
            var allProviders = await _bookingService.GetAllProvidersAsync();
            return $"No se encontró proveedor que coincida con '{providerQuery}'. Disponibles: {string.Join(", ", allProviders.Select(p => p.Name))}";
        }

        var results = new List<string>();

        foreach (var provider in matchingProviders)
        {
            var slots = await _bookingService.GetAvailableSlotsAsync(provider.Id, date);
            var availableSlots = slots.Where(s => s.IsAvailable).ToList();

            if (availableSlots.Any())
            {
                var times = availableSlots.Select(s => s.Time.ToString(@"hh\:mm")).ToList();
                results.Add($"• {provider.Name} ({provider.Role}): {string.Join(", ", times)}");
            }
        }

        if (!results.Any())
            return $"No hay horarios disponibles para {date:yyyy-MM-dd}";

        return $"Horarios disponibles para {date:yyyy-MM-dd}:\n{string.Join("\n", results)}";
    }

    [KernelFunction]
    [Description("Busca la primera cita disponible con cualquier proveedor desde hoy hacia adelante")]
    public async Task<string> GetFirstAvailableAppointment(
        [Description("Número de días hacia adelante a buscar (default: 30)")] int daysToSearch = 30)
    {
        var today = DateTime.Now.Date;
        var allProviders = await _bookingService.GetAllProvidersAsync();

        for (int i = 0; i < daysToSearch; i++)
        {
            var date = today.AddDays(i);

            foreach (var provider in allProviders)
            {
                var slots = await _bookingService.GetAvailableSlotsAsync(provider.Id, date);
                var availableSlots = slots.Where(s => s.IsAvailable).ToList();

                if (availableSlots.Any())
                {
                    var times = availableSlots.Select(s => s.Time.ToString(@"hh\:mm")).Take(5).ToList();
                    return $"Primera cita disponible:\n" +
                           $"Proveedor: {provider.Name} ({provider.Role})\n" +
                           $"Fecha: {date:yyyy-MM-dd} ({date:dddd})\n" +
                           $"Horarios: {string.Join(", ", times)}";
                }
            }
        }

        return $"No hay disponibilidad en los próximos {daysToSearch} días";
    }

    [KernelFunction]
    [Description("Agenda una nueva cita. Si faltan datos, devuelve un error descriptivo.")]
    public async Task<string> BookAppointment(
        [Description("Nombre completo del cliente")] string clientName,
        [Description("Telefono celular del cliente")] string clientPhone,
        [Description("Correo electronico del cliente (Opcional, usar 'no-email' si no se tiene)")] string clientEmail,
        [Description("Nombre del proveedor (ej: 'Dr. Ramírez') o ID (ej: 'DR001')")] string providerNameOrId,
        [Description("Fecha para agendar la cita. Formato YYYY-MM-DD")] string stringDate,
        [Description("Horario para agendar la cita. Formato 24 horas HH:MM")] string stringTime,
        [Description("Razon de la cita")] string reason)
    {
        var invalidTerms = new[] { "no email", "no-email", "unknown", "no nombre", "string", "user", "no phone" };

        if (string.IsNullOrWhiteSpace(clientName) || invalidTerms.Any(t => clientName.Contains(t, StringComparison.OrdinalIgnoreCase)))
            return "FALLO DE VALIDACIÓN: Falta el NOMBRE del cliente. NO inventes valores. PREGUNTA al usuario su nombre.";

        if (string.IsNullOrWhiteSpace(clientEmail) || !clientEmail.Contains('@') || invalidTerms.Any(t => clientEmail.Contains(t, StringComparison.OrdinalIgnoreCase)))
            return "FALLO DE VALIDACIÓN: Falta un EMAIL válido. NO uses 'no-email'. PREGUNTA al usuario su correo.";

        if (string.IsNullOrWhiteSpace(clientPhone) || invalidTerms.Any(t => clientPhone.Contains(t, StringComparison.OrdinalIgnoreCase)))
            return "FALLO DE VALIDACIÓN: Falta el TELÉFONO. PREGUNTA al usuario su número.";

        try
        {
            if (!DateTime.TryParse(stringDate, out var date))
                return $"FALLO: La fecha '{stringDate}' no es válida. Usa formato YYYY-MM-DD.";

            if (!TimeSpan.TryParse(stringTime, out var time))
                return $"FALLO: La hora '{stringTime}' no es válida. Usa formato HH:MM (24h).";

            var matchingProviders = await _bookingService.SearchProvidersAsync(providerNameOrId);

            if (matchingProviders.Count == 0)
            {
                var allProviders = await _bookingService.GetAllProvidersAsync();
                return $"FALLO: No se encontró proveedor '{providerNameOrId}'. Disponibles: {string.Join(", ", allProviders.Select(p => p.Name))}";
            }

            if (matchingProviders.Count > 1)
                return $"FALLO: Múltiples proveedores encontrados para '{providerNameOrId}': {string.Join(", ", matchingProviders.Select(p => p.Name))}. Por favor sea más específico.";

            var provider = matchingProviders.First();

            var customFields = new Dictionary<string, object>
            {
                ["phone"] = clientPhone,
                ["email"] = clientEmail,
                ["reason"] = reason
            };

            var booking = await _bookingService.CreateBookingAsync(
                clientName,
                provider.Id,
                date,
                time,
                customFields);

            if (booking != null)
            {
                return $"ÉXITO: Cita confirmada exitosamente. \n" +
                       $"Código de Confirmación: {booking.ConfirmationCode} \n" +
                       $"Cliente: {clientName} \n" +
                       $"Proveedor: {provider.Name} ({provider.Role})\n" +
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
    [Description("Cancelar una cita mediante codigo de confirmacion.")]
    public async Task<string> CancelAppointment(
        [Description("Codigo de confirmacion de la cita")] string confirmationCode)
    {
        var booking = await _bookingService.GetBookingAsync(confirmationCode);
        if (booking == null)
            return $"La cita con el código {confirmationCode} no fue encontrada, pruebe nuevamente.";

        var success = await _bookingService.CancelBookingAsync(confirmationCode);

        if (success)
            return $"✓ Cita cancelada: {booking.ClientName}, " +
                   $"{booking.ScheduledDate:yyyy-MM-dd} {booking.ScheduledTime:hh\\:mm}";

        return "Error al cancelar la cita";
    }

    [KernelFunction]
    [Description("Obtener informacion de una cita. Requiere código Y nombre del cliente para verificación.")]
    public async Task<string> GetAppointmentInfo(
        [Description("Codigo de confirmacion de la cita")] string confirmationCode,
        [Description("Nombre del cliente para verificación de identidad")] string clientName)
    {
        var booking = await _bookingService.GetBookingAsync(confirmationCode);
        if (booking == null)
            return $"La cita con el código {confirmationCode} no fue encontrada, pruebe nuevamente.";

        // Verificación de identidad: el nombre debe coincidir
        if (string.IsNullOrWhiteSpace(clientName) ||
            !booking.ClientName.Contains(clientName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "No se puede verificar la identidad. El nombre proporcionado no coincide con el registro. " +
                   "Por favor, confirme el nombre completo asociado a esta cita.";
        }

        return $"Cita {booking.ConfirmationCode}:\n" +
               $"Cliente: {booking.ClientName}\n" +
               $"Proveedor: {booking.ProviderName}\n" +
               $"Fecha: {booking.ScheduledDate:yyyy-MM-dd}\n" +
               $"Hora: {booking.ScheduledTime:hh\\:mm}\n" +
               $"Estado: {booking.Status}";
    }

    [KernelFunction]
    [Description("Lista la ocupación de citas para hoy (sin datos de clientes por privacidad).")]
    public async Task<string> GetAllAppointmentsByDate()
    {
        var bookings = await _bookingService.GetBookingsByDateAsync(DateTime.Now.Date);
        if (!bookings.Any())
            return "No hay citas agendadas para hoy";

        // SEGURIDAD: No exponer nombres de clientes
        return $"Citas agendadas para hoy ({bookings.Count} total):\n" +
               string.Join("\n", bookings.Select(b =>
                   $"- {b.ScheduledTime:hh\\:mm} con {b.ProviderName} ({b.Status})"));
    }
}
