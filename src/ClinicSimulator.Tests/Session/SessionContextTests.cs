using ClinicSimulator.AI.Plugins;
using ClinicSimulator.Core.Adapters;
using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Services;
using ClinicSimulator.Core.Session;
using Xunit;

namespace ClinicSimulator.Tests.Session;

public class SessionContextTests
{
    private readonly SessionContext _sessionContext = new();

    // ===== Test 1: Validate patient by document ID =====
    [Fact]
    public void ValidatePatientId_ShouldTrackInSession()
    {
        _sessionContext.ValidatePatientId("CC-123456");

        Assert.Equal("CC-123456", _sessionContext.ValidatedPatientId);
        Assert.True(_sessionContext.IsPatientValidated("CC-123456"));
    }

    // ===== Test 2: Validate booking by confirmation code =====
    [Fact]
    public void ValidateConfirmationCode_ShouldTrackInSession()
    {
        _sessionContext.ValidateConfirmationCode("CITA-AB12");

        Assert.True(_sessionContext.IsCodeValidated("CITA-AB12"));
        Assert.Contains("CITA-AB12", _sessionContext.ValidatedConfirmationCodes);
    }

    // ===== Test 3: Reject access when patientId not validated =====
    [Fact]
    public void IsPatientValidated_WithoutValidation_ShouldReturnFalse()
    {
        Assert.False(_sessionContext.IsPatientValidated("CC-999999"));
        Assert.Null(_sessionContext.ValidatedPatientId);
    }

    // ===== Test 4: Reject access when confirmation code not validated =====
    [Fact]
    public void IsCodeValidated_WithoutValidation_ShouldReturnFalse()
    {
        Assert.False(_sessionContext.IsCodeValidated("CITA-XXXX"));
        Assert.Empty(_sessionContext.ValidatedConfirmationCodes);
    }

    // ===== Test 5: Multiple validations accumulate in session =====
    [Fact]
    public void MultipleValidations_ShouldAccumulateInSession()
    {
        _sessionContext.ValidatePatientId("CC-111111");
        _sessionContext.ValidateConfirmationCode("CITA-AA11");
        _sessionContext.ValidateConfirmationCode("CITA-BB22");

        Assert.True(_sessionContext.IsPatientValidated("CC-111111"));
        Assert.True(_sessionContext.IsCodeValidated("CITA-AA11"));
        Assert.True(_sessionContext.IsCodeValidated("CITA-BB22"));
        Assert.Equal(2, _sessionContext.ValidatedConfirmationCodes.Count);
    }

    // ===== Test 6: GetBookingByPatientIdAsync returns matching booking =====
    [Fact]
    public async Task GetBookingByPatientIdAsync_ShouldReturnMatchingBooking()
    {
        var providers = new List<ServiceProvider>
        {
            new() { Id = "DR001", Name = "Dr. Test", Role = "General",
                     WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday, DayOfWeek.Friday],
                     StartTime = TimeSpan.FromHours(9),
                     EndTime = TimeSpan.FromHours(17), SlotDurationMinutes = 30 }
        };
        var adapter = new InMemoryClientAdapter(providers);
        var service = new BookingService(adapter);

        var tomorrow = DateTime.Today.AddDays(1);
        while (!providers[0].WorkingDays.Contains(tomorrow.DayOfWeek))
            tomorrow = tomorrow.AddDays(1);

        var booking = await service.CreateBookingAsync(
            "Juan Pérez", "DR001", tomorrow, TimeSpan.FromHours(10),
            new Dictionary<string, object> { ["patientId"] = "CC-123456" });

        var found = await service.GetBookingByPatientIdAsync("CC-123456");

        Assert.NotNull(found);
        Assert.Equal(booking.ConfirmationCode, found.ConfirmationCode);
        Assert.Equal("Juan Pérez", found.ClientName);
    }

    // ===== Test 7: GetBookingsByPatientIdAsync returns all patient bookings =====
    [Fact]
    public async Task GetBookingsByPatientIdAsync_ShouldReturnAllPatientBookings()
    {
        var providers = new List<ServiceProvider>
        {
            new() { Id = "DR001", Name = "Dr. Test", Role = "General",
                     WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday, DayOfWeek.Friday],
                     StartTime = TimeSpan.FromHours(9),
                     EndTime = TimeSpan.FromHours(17), SlotDurationMinutes = 30 }
        };
        var adapter = new InMemoryClientAdapter(providers);
        var service = new BookingService(adapter);

        var day1 = DateTime.Today.AddDays(1);
        while (!providers[0].WorkingDays.Contains(day1.DayOfWeek))
            day1 = day1.AddDays(1);
        var day2 = day1.AddDays(1);
        while (!providers[0].WorkingDays.Contains(day2.DayOfWeek))
            day2 = day2.AddDays(1);

        await service.CreateBookingAsync(
            "Juan Pérez", "DR001", day1, TimeSpan.FromHours(10),
            new Dictionary<string, object> { ["patientId"] = "CC-123456" });
        await service.CreateBookingAsync(
            "Juan Pérez", "DR001", day2, TimeSpan.FromHours(11),
            new Dictionary<string, object> { ["patientId"] = "CC-123456" });
        await service.CreateBookingAsync(
            "María López", "DR001", day1, TimeSpan.FromHours(14),
            new Dictionary<string, object> { ["patientId"] = "CC-999999" });

        var juanBookings = await service.GetBookingsByPatientIdAsync("CC-123456");
        var mariaBookings = await service.GetBookingsByPatientIdAsync("CC-999999");

        Assert.Equal(2, juanBookings.Count);
        Assert.Single(mariaBookings);
    }

    // ===== Test 8: BookAppointment stores patientId and auto-validates session =====
    [Fact]
    public async Task BookAppointment_ShouldStorePatientIdAndAutoValidate()
    {
        var providers = new List<ServiceProvider>
        {
            new() { Id = "DR001", Name = "Dr. Ramírez", Role = "Oftalmología",
                     WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday, DayOfWeek.Friday],
                     StartTime = TimeSpan.FromHours(9),
                     EndTime = TimeSpan.FromHours(17), SlotDurationMinutes = 30 }
        };
        var adapter = new InMemoryClientAdapter(providers);
        var service = new BookingService(adapter);
        var sessionContext = new SessionContext();
        var plugin = new BookingPlugin(service, sessionContext);

        var tomorrow = DateTime.Today.AddDays(1);
        // Asegurar que el día es laboral
        while (!providers[0].WorkingDays.Contains(tomorrow.DayOfWeek))
            tomorrow = tomorrow.AddDays(1);

        var result = await plugin.BookAppointment(
            clientName: "Juan Pérez",
            patientId: "CC-123456",
            clientPhone: "3001234567",
            clientEmail: "juan@test.com",
            providerNameOrId: "Ramírez",
            stringDate: tomorrow.ToString("yyyy-MM-dd"),
            stringTime: "10:00",
            reason: "Control general");

        Assert.Contains("ÉXITO", result);
        Assert.Contains("CC-123456", result);

        // Verificar que el sessionContext fue auto-validado
        Assert.True(sessionContext.IsPatientValidated("CC-123456"));
        Assert.NotEmpty(sessionContext.ValidatedConfirmationCodes);
    }

    // ===== Test 9: CancelAppointment rejects unauthorized access =====
    [Fact]
    public async Task CancelAppointment_WithoutValidation_ShouldDenyAccess()
    {
        var providers = new List<ServiceProvider>
        {
            new() { Id = "DR001", Name = "Dr. Ramírez", Role = "Oftalmología",
                     WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday, DayOfWeek.Friday],
                     StartTime = TimeSpan.FromHours(9),
                     EndTime = TimeSpan.FromHours(17), SlotDurationMinutes = 30 }
        };
        var adapter = new InMemoryClientAdapter(providers);
        var service = new BookingService(adapter);

        var tomorrow = DateTime.Today.AddDays(1);
        while (!providers[0].WorkingDays.Contains(tomorrow.DayOfWeek))
            tomorrow = tomorrow.AddDays(1);

        var booking = await service.CreateBookingAsync(
            "Juan Pérez", "DR001", tomorrow, TimeSpan.FromHours(10),
            new Dictionary<string, object> { ["patientId"] = "CC-123456" });

        // Usar un SessionContext NUEVO (sin validación previa)
        var sessionContext = new SessionContext();
        var plugin = new BookingPlugin(service, sessionContext);

        var result = await plugin.CancelAppointment(booking.ConfirmationCode);

        Assert.Contains("ACCESO DENEGADO", result);
    }

    // ===== Test 10: PatientId validation is case-insensitive =====
    [Fact]
    public void PatientIdValidation_ShouldBeCaseInsensitive()
    {
        _sessionContext.ValidatePatientId("cc-123456");

        Assert.True(_sessionContext.IsPatientValidated("CC-123456"));
        Assert.True(_sessionContext.IsPatientValidated("cc-123456"));
        Assert.True(_sessionContext.IsPatientValidated("Cc-123456"));
    }
}
