using ReceptionistAgent.AI.Plugins;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;
using ReceptionistAgent.Core.Session;
using Moq;
using Xunit;

namespace ReceptionistAgent.Tests.Security;

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

        var sessionContext = new SessionContext();
        var tenantContext = new TenantContext();
        var plugin = new BookingPlugin(_mockService.Object, sessionContext, tenantContext);

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
    public async Task GetAppointmentInfo_WithoutValidation_ShouldDenyAccess()
    {
        var booking = new BookingRecord
        {
            ConfirmationCode = "ABC123",
            ClientName = "Juan Pérez",
            ProviderName = "Dr. Ramírez",
            ScheduledDate = DateTime.Today,
            ScheduledTime = new TimeSpan(10, 0, 0),
            CustomFields = new Dictionary<string, object> { ["clientId"] = "CC-123456" }
        };

        _mockService.Setup(s => s.GetBookingAsync("ABC123"))
            .ReturnsAsync(booking);

        // SessionContext sin validar → debería denegar acceso
        var sessionContext = new SessionContext();
        var tenantContext = new TenantContext();
        var plugin = new BookingPlugin(_mockService.Object, sessionContext, tenantContext);

        var result = await plugin.GetAppointmentInfo(confirmationCode: "ABC123");

        Assert.Contains("ACCESO DENEGADO", result);
        Assert.DoesNotContain("Juan Pérez", result);
    }

    [Fact]
    public async Task GetAppointmentInfo_WithValidatedClientId_ShouldAllowAccess()
    {
        var booking = new BookingRecord
        {
            ConfirmationCode = "ABC123",
            ClientName = "Juan Pérez",
            ProviderName = "Dr. Ramírez",
            ScheduledDate = DateTime.Today,
            ScheduledTime = new TimeSpan(10, 0, 0),
            Status = BookingStatus.Scheduled,
            CustomFields = new Dictionary<string, object> { ["clientId"] = "CC-123456" }
        };

        _mockService.Setup(s => s.GetBookingAsync("ABC123"))
            .ReturnsAsync(booking);

        // Pre-validar por código + clientId
        var sessionContext = new SessionContext();
        var tenantContext = new TenantContext();
        var plugin = new BookingPlugin(_mockService.Object, sessionContext, tenantContext);

        var result = await plugin.GetAppointmentInfo(confirmationCode: "ABC123", clientId: "CC-123456");

        Assert.Contains("Juan Pérez", result);
        Assert.Contains("Dr. Ramírez", result);
        Assert.Contains("ABC123", result);
    }
}
