using Moq;
using ReceptionistAgent.Connectors.Adapters;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;
using Xunit;
using Microsoft.Extensions.Logging;

namespace ReceptionistAgent.Tests.Adapters;

public class SqlClientDataAdapterTests
{
    private const string TestConnectionString = "Server=(localdb)\\mssqllocaldb;Database=ReceptionistAgentTestDb;Trusted_Connection=True;";

    [Fact(Skip = "Requiere LocalDB o SQL Server corriendo para integrarse adecuadamente. Quitar Skip para probar localmente.")]
    public async Task CreateBookingAsync_ShouldInsertRecord()
    {
        // Arrange
        var mockBackup = new Mock<IBookingBackupService>();
        var mockLogger = new Mock<ILogger<SqlClientDataAdapter>>();
        var adapter = new SqlClientDataAdapter(TestConnectionString, mockBackup.Object, mockLogger.Object);
        var booking = new BookingRecord
        {
            TenantId = "TEST-TENANT",
            ClientName = "Juan Pérez",
            ProviderId = "DR001",
            ProviderName = "Dr. Test",
            ScheduledDate = DateTime.UtcNow.Date.AddDays(1),
            ScheduledTime = new TimeSpan(10, 0, 0),
            Status = BookingStatus.Scheduled
        };

        // Act
        var created = await adapter.CreateBookingAsync(booking);

        // Assert
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.StartsWith("CITA-", created.ConfirmationCode);

        // Clean up (assuming tests drop/delete records)
        await adapter.DeleteBookingAsync(created.Id.ToString());
    }
}
