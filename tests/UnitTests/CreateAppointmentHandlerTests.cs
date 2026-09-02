using Application.Appointments.CreateAppointment;
using Application.Common;
using Domain.Entities;
using Domain.Services;
using Moq;

namespace UnitTests;

public class CreateAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _repository = new();
    private readonly ConflictChecker _conflictChecker = new();

    private CreateAppointmentHandler CreateHandler() => new(_repository.Object, _conflictChecker);

    [Fact]
    public async Task Handle_CreatesAppointment_WhenNoConflict()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());

        var command = new CreateAppointmentCommand(
            "Jane Doe", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Appointment);
        Assert.Equal("Jane Doe", result.Appointment!.CustomerName);
        _repository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenTimeOverlapsExisting()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existing = new Appointment
        {
            Id = Guid.NewGuid(),
            CustomerName = "Existing Customer",
            Start = baseTime,
            End = baseTime.AddMinutes(30)
        };

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { existing });

        var command = new CreateAppointmentCommand(
            "Jane Doe", null, null,
            baseTime.AddMinutes(15), baseTime.AddMinutes(45), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(result.Conflicts);
        _repository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenProposedTimeIsBackToBackWithExisting()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existing = new Appointment
        {
            Id = Guid.NewGuid(),
            CustomerName = "Existing Customer",
            Start = baseTime,
            End = baseTime.AddMinutes(30)
        };

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { existing });

        // Starts exactly when the existing appointment ends - should NOT conflict
        var command = new CreateAppointmentCommand(
            "Jane Doe", null, null,
            baseTime.AddMinutes(30), baseTime.AddMinutes(60), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        _repository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
