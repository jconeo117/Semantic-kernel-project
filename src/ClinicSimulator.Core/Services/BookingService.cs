using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Adapters;

namespace ClinicSimulator.Core.Services;

public class BookingService : IBookingService
{
    private readonly IClientDataAdapter _adapter;

    public BookingService(IClientDataAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<List<TimeSlot>> GetAvailableSlotsAsync(string providerId, DateTime date)
    {
        var providers = await _adapter.GetAllProvidersAsync();
        var provider = providers.FirstOrDefault(p => p.Id == providerId)
            ?? throw new ArgumentException("Proveedor no encontrado");

        if (!provider.WorkingDays.Contains(date.DayOfWeek))
            return [];

        var slots = new List<TimeSlot>();
        var currentTime = provider.StartTime;
        var slotDuration = TimeSpan.FromMinutes(provider.SlotDurationMinutes);

        while (currentTime < provider.EndTime)
        {
            var slot = new TimeSlot
            {
                Date = date,
                Time = currentTime,
                IsAvailable = !await _adapter.ExistsAsync(date, currentTime, providerId)
            };

            slots.Add(slot);
            currentTime = currentTime.Add(slotDuration);
        }

        return slots;
    }

    public async Task<BookingRecord> CreateBookingAsync(
        string clientName,
        string providerId,
        DateTime date,
        TimeSpan time,
        Dictionary<string, object>? customFields = null)
    {
        if (await _adapter.ExistsAsync(date, time, providerId))
        {
            throw new InvalidOperationException("El horario ya está ocupado");
        }

        var providers = await _adapter.GetAllProvidersAsync();
        var provider = providers.FirstOrDefault(p => p.Id == providerId)
            ?? throw new Exception("Proveedor no encontrado");

        if (!provider.WorkingDays.Contains(date.DayOfWeek))
        {
            throw new InvalidOperationException("El proveedor no trabaja en esa fecha");
        }

        if (time < provider.StartTime || time > provider.EndTime)
        {
            throw new InvalidOperationException("El horario está fuera del rango del proveedor");
        }

        var booking = new BookingRecord
        {
            ClientName = clientName,
            ProviderId = providerId,
            ProviderName = provider.Name,
            ScheduledDate = date,
            ScheduledTime = time,
            Status = BookingStatus.Confirmed,
            CustomFields = customFields ?? new Dictionary<string, object>()
        };

        return await _adapter.CreateBookingAsync(booking);
    }

    public async Task<bool> CancelBookingAsync(string confirmationCode)
    {
        var booking = await _adapter.GetBookingByCodeAsync(confirmationCode);
        if (booking == null) return false;
        booking.Status = BookingStatus.Cancelled;
        return await _adapter.UpdateBookingAsync(booking);
    }

    public async Task<BookingRecord?> GetBookingAsync(string confirmationCode)
    {
        return await _adapter.GetBookingByCodeAsync(confirmationCode);
    }

    public async Task<List<BookingRecord>> GetBookingsByDateAsync(DateTime date)
    {
        return await _adapter.GetBookingsByDateAsync(date);
    }

    public async Task<BookingRecord?> GetBookingByPatientIdAsync(string patientId)
    {
        return await _adapter.GetBookingByPatientIdAsync(patientId);
    }

    public async Task<List<BookingRecord>> GetBookingsByPatientIdAsync(string patientId)
    {
        return await _adapter.GetBookingsByPatientIdAsync(patientId);
    }

    public async Task<List<ServiceProvider>> GetAllProvidersAsync()
    {
        return await _adapter.GetAllProvidersAsync();
    }

    public async Task<List<ServiceProvider>> SearchProvidersAsync(string query)
    {
        return await _adapter.SearchProvidersAsync(query);
    }
}
