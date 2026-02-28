using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using Xunit;

namespace ReceptionistAgent.Tests.Adapters;

public class InMemoryClientAdapterTests
{
    private readonly InMemoryClientAdapter _adapter;
    private readonly List<ServiceProvider> _testProviders;

    public InMemoryClientAdapterTests()
    {
        _testProviders =
        [
            new ServiceProvider
            {
                Id = "PRV001",
                Name = "Dr. Carlos Ramírez",
                Role = "Oftalmología General",
                WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday],
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0)
            },
            new ServiceProvider
            {
                Id = "PRV002",
                Name = "Dra. María González",
                Role = "Retina",
                WorkingDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(16, 0, 0)
            }
        ];

        _adapter = new InMemoryClientAdapter(_testProviders);
    }

    // === CreateBookingAsync ===

    [Fact]
    public async Task CreateBookingAsync_ShouldAssignIdAndCode()
    {
        // Arrange
        var booking = new BookingRecord
        {
            ClientName = "Juan Pérez",
            ProviderId = "PRV001",
            ProviderName = "Dr. Carlos Ramírez",
            ScheduledDate = DateTime.Now.Date,
            ScheduledTime = new TimeSpan(10, 0, 0),
            Status = BookingStatus.Confirmed
        };

        // Act
        var result = await _adapter.CreateBookingAsync(booking);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.StartsWith("CITA-", result.ConfirmationCode);
        Assert.NotEqual(default, result.CreatedAt);
    }

    // === GetBookingByCodeAsync ===

    [Fact]
    public async Task GetBookingByCodeAsync_ShouldReturnCorrectBooking()
    {
        // Arrange
        var booking = new BookingRecord
        {
            ClientName = "Juan Pérez",
            ProviderId = "PRV001",
            ScheduledDate = DateTime.Now.Date,
            ScheduledTime = new TimeSpan(10, 0, 0)
        };
        var created = await _adapter.CreateBookingAsync(booking);

        // Act
        var result = await _adapter.GetBookingByCodeAsync(created.ConfirmationCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Juan Pérez", result.ClientName);
    }

    [Fact]
    public async Task GetBookingByCodeAsync_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var result = await _adapter.GetBookingByCodeAsync("CITA-NOEXISTE");

        // Assert
        Assert.Null(result);
    }

    // === ExistsAsync ===

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenBookingExists()
    {
        // Arrange
        var date = DateTime.Now.Date;
        var time = new TimeSpan(10, 0, 0);
        var providerId = "PRV001";

        await _adapter.CreateBookingAsync(new BookingRecord
        {
            ClientName = "Test",
            ProviderId = providerId,
            ScheduledDate = date,
            ScheduledTime = time,
            Status = BookingStatus.Confirmed
        });

        // Act
        var exists = await _adapter.ExistsAsync(date, time, providerId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldIgnoreCancelledBookings()
    {
        // Arrange
        var date = DateTime.Now.Date;
        var time = new TimeSpan(10, 0, 0);
        var providerId = "PRV001";

        var booking = await _adapter.CreateBookingAsync(new BookingRecord
        {
            ClientName = "Test",
            ProviderId = providerId,
            ScheduledDate = date,
            ScheduledTime = time,
            Status = BookingStatus.Confirmed
        });

        booking.Status = BookingStatus.Cancelled;
        await _adapter.UpdateBookingAsync(booking);

        // Act
        var exists = await _adapter.ExistsAsync(date, time, providerId);

        // Assert
        Assert.False(exists);
    }

    // === SearchProvidersAsync ===

    [Fact]
    public async Task SearchProvidersAsync_ShouldFindByName()
    {
        // Act
        var result = await _adapter.SearchProvidersAsync("Ramirez");

        // Assert
        Assert.Single(result);
        Assert.Equal("PRV001", result.First().Id);
    }

    [Fact]
    public async Task SearchProvidersAsync_ShouldFindByRole()
    {
        // Act
        var result = await _adapter.SearchProvidersAsync("Retina");

        // Assert
        Assert.Single(result);
        Assert.Equal("PRV002", result.First().Id);
    }

    // === GetAllProvidersAsync ===

    [Fact]
    public async Task GetAllProvidersAsync_ShouldReturnAll()
    {
        // Act
        var result = await _adapter.GetAllProvidersAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }
}
