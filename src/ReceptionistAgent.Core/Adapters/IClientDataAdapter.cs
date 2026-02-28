using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Core.Adapters;

/// <summary>
/// Contrato único para leer/escribir datos del cliente.
/// Cada tenant/cliente implementa o configura su propio adapter
/// que conecta con su base de datos externa.
/// </summary>
public interface IClientDataAdapter
{
    // === Bookings ===
    Task<BookingRecord> CreateBookingAsync(BookingRecord booking);
    Task<BookingRecord?> GetBookingByCodeAsync(string confirmationCode);
    Task<List<BookingRecord>> GetBookingsByDateAsync(DateTime date);
    Task<List<BookingRecord>> GetAllBookingsAsync();
    Task<bool> UpdateBookingAsync(BookingRecord booking);
    Task<bool> DeleteBookingAsync(string id);
    Task<bool> ExistsAsync(DateTime date, TimeSpan time, string providerId);

    // === Client Lookups ===
    Task<BookingRecord?> GetBookingByClientIdAsync(string clientId);
    Task<List<BookingRecord>> GetBookingsByClientIdAsync(string clientId);

    // === Service Providers ===
    Task<List<ServiceProvider>> GetAllProvidersAsync();
    Task<List<ServiceProvider>> SearchProvidersAsync(string query);
}
