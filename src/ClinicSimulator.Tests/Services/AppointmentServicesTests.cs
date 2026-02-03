using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Services;
using ClinicSimulator.Core.Repositories;
using Moq;
using Xunit;

namespace ClinicSimulator.Tests.Services;

public class AppointmentServicesTests
{
    private readonly Mock<IAppointmentRepository> _mockRepo;
    private readonly AppointmentServices _service;

    public AppointmentServicesTests()
    {
        _mockRepo = new Mock<IAppointmentRepository>();
        _service = new AppointmentServices(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldReturnSlots_WhenDoctorWorksOnDate()
    {
        // Arrange
        var doctorId = "DR-0001"; // Monday, Tuesday, Wednesday, Thursday, Friday
        var date = new DateTime(2023, 10, 23); // Monday

        _mockRepo.Setup(r => r.ExistsAsync(It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<string>()))
            .ReturnsAsync(false); // No appointments booked

        // Act
        var result = await _service.GetAvailableSlotsAsync(doctorId, date);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, slot => Assert.True(slot.IsAvailable));
        Assert.Equal(date, result.First().Date);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldReturnEmpty_WhenDoctorDoesNotWorkOnDate()
    {
        // Arrange
        var doctorId = "DR-0002"; // Mon, Wed, Fri
        var date = new DateTime(2023, 10, 24); // Tuesday

        // Act
        var result = await _service.GetAvailableSlotsAsync(doctorId, date);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldMarkSlotAsUnavailable_WhenBooked()
    {
        // Arrange
        var doctorId = "DR-0001";
        var date = new DateTime(2023, 10, 23);
        var bookedTime = new TimeSpan(9, 0, 0);

        _mockRepo.Setup(r => r.ExistsAsync(date, bookedTime, doctorId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.GetAvailableSlotsAsync(doctorId, date);

        // Assert
        var bookedSlot = result.First(s => s.Time == bookedTime);
        Assert.False(bookedSlot.IsAvailable);
    }

    [Fact]
    public async Task BookAppointmentAsync_ShouldCreateAppointment_WhenSlotIsAvailable()
    {
        // Arrange
        var doctorId = "DR-0001";
        var date = new DateTime(2023, 10, 23);
        var time = new TimeSpan(10, 0, 0);

        _mockRepo.Setup(r => r.ExistsAsync(date, time, doctorId))
            .ReturnsAsync(false);

        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment a) => a);

        // Act
        var result = await _service.BookAppointmentAsync("John Doe", "123456789", "john@test.com", doctorId, date, time, "Checkup");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(doctorId, result.DoctorId);
        Assert.Equal(date, result.AppointmentDate);
        Assert.Equal(time, result.AppointmentTime);
        Assert.Equal(AppointmentStatus.Confirmed, result.Status);
    }

    [Fact]
    public async Task BookAppointmentAsync_ShouldThrow_WhenSlotIsOccupied()
    {
        // Arrange
        var doctorId = "DR-0001";
        var date = new DateTime(2023, 10, 23);
        var time = new TimeSpan(10, 0, 0);

        _mockRepo.Setup(r => r.ExistsAsync(date, time, doctorId))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.BookAppointmentAsync("John Doe", "123456789", "john@test.com", doctorId, date, time, "Checkup"));
    }

    [Fact]
    public async Task BookAppointmentAsync_ShouldThrow_WhenDoctorDoesNotWorkOnDate()
    {
        // Arrange
        var doctorId = "DR-0002"; // Mon, Wed, Fri
        var date = new DateTime(2023, 10, 24); // Tuesday
        var time = new TimeSpan(10, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.BookAppointmentAsync("John Doe", "123456789", "john@test.com", doctorId, date, time, "Checkup"));
    }
}
