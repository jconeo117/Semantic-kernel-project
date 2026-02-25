using ClinicSimulator.Core.Models;

namespace ClinicSimulator.Core.Services;

public interface IBookingService
{
    Task<List<TimeSlot>> GetAvailableSlotsAsync(string providerId, DateTime date);
    Task<BookingRecord> CreateBookingAsync(
        string clientName,
        string providerId,
        DateTime date,
        TimeSpan time,
        Dictionary<string, object>? customFields = null);
    Task<bool> CancelBookingAsync(string confirmationCode);
    Task<BookingRecord?> GetBookingAsync(string confirmationCode);
    Task<List<BookingRecord>> GetBookingsByDateAsync(DateTime date);

    Task<List<ServiceProvider>> GetAllProvidersAsync();
    Task<List<ServiceProvider>> SearchProvidersAsync(string query);
}
