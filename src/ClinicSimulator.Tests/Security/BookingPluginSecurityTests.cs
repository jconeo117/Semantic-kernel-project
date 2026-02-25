using ClinicSimulator.AI.Plugins;
using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Services;
using Moq;
using Xunit;

namespace ClinicSimulator.Tests.Security;

public class BookingPluginSecurityTests
{
    private readonly Mock<IBookingService> _mockService = new();

    [Fact]
    public async Task GetAllAppointmentsByDate_ShouldNotExposeClientNames()
    {
        var bookings = new List<BookingRecord>
        {
            new()
            {
                ConfirmationCode = "ABC123",
                ClientName = "Juan Pérez",
                ProviderName = "Dr. Ramírez",
                ScheduledDate = DateTime.Today,
                ScheduledTime = new TimeSpan(10, 0, 0),
                Status = BookingStatus.Scheduled
            },
            new()
            {
                ConfirmationCode = "DEF456",
                ClientName = "María López",
                ProviderName = "Dra. García",
                ScheduledDate = DateTime.Today,
                ScheduledTime = new TimeSpan(14, 30, 0),
                Status = BookingStatus.Confirmed
            }
        };

        _mockService.Setup(s => s.GetBookingsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(bookings);

        var plugin = new BookingPlugin(_mockService.Object);

        var result = await plugin.GetAllAppointmentsByDate();

        // NO debe contener nombres de clientes
        Assert.DoesNotContain("Juan Pérez", result);
        Assert.DoesNotContain("María López", result);

        // SÍ debe contener info de proveedores y horarios
        Assert.Contains("Dr. Ramírez", result);
        Assert.Contains("Dra. García", result);
        Assert.Contains("2 total", result);
    }

    [Fact]
    public async Task GetAppointmentInfo_WithWrongName_ShouldDenyAccess()
    {
        var booking = new BookingRecord
        {
            ConfirmationCode = "ABC123",
            ClientName = "Juan Pérez",
            ProviderName = "Dr. Ramírez",
            ScheduledDate = DateTime.Today,
            ScheduledTime = new TimeSpan(10, 0, 0)
        };

        _mockService.Setup(s => s.GetBookingAsync("ABC123"))
            .ReturnsAsync(booking);

        var plugin = new BookingPlugin(_mockService.Object);

        // Nombre incorrecto
        var result = await plugin.GetAppointmentInfo("ABC123", "Carlos García");

        Assert.Contains("No se puede verificar la identidad", result);
        Assert.DoesNotContain("Juan Pérez", result);
    }

    [Fact]
    public async Task GetAppointmentInfo_WithCorrectName_ShouldAllowAccess()
    {
        var booking = new BookingRecord
        {
            ConfirmationCode = "ABC123",
            ClientName = "Juan Pérez",
            ProviderName = "Dr. Ramírez",
            ScheduledDate = DateTime.Today,
            ScheduledTime = new TimeSpan(10, 0, 0),
            Status = BookingStatus.Scheduled
        };

        _mockService.Setup(s => s.GetBookingAsync("ABC123"))
            .ReturnsAsync(booking);

        var plugin = new BookingPlugin(_mockService.Object);

        // Nombre parcial correcto (verificación case-insensitive)
        var result = await plugin.GetAppointmentInfo("ABC123", "juan");

        Assert.Contains("Juan Pérez", result);
        Assert.Contains("Dr. Ramírez", result);
        Assert.Contains("ABC123", result);
    }
}
