using Application.Appointments.UpdateAppointment;
using Application.Common;
using Domain.Entities;
using Domain.Services;
using Moq;

namespace UnitTests;

public class UpdateAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _repository = new();
    private readonly ConflictChecker _conflictChecker = new();

    private UpdateAppointmentHandler CreateHandler() => new(_repository.Object, _conflictChecker);

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenAppointmentDoesNotExist()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var command = new UpdateAppointmentCommand(
            Guid.NewGuid(), "Jane Doe", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.NotFound);
        Assert.False(result.Success);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenNewTimeOverlapsAnotherAppointment()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var toUpdate = new Appointment { Id = Guid.NewGuid(), CustomerName = "Erik", Start = baseTime.AddHours(5), End = baseTime.AddHours(5).AddMinutes(30) };
        var other = new Appointment { Id = Guid.NewGuid(), CustomerName = "Jane Doe", Start = baseTime, End = baseTime.AddMinutes(30) };

        _repository.Setup(r => r.GetByIdAsync(toUpdate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toUpdate);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { toUpdate, other });

        // Try to move Erik's appointment into Jane's slot
        var command = new UpdateAppointmentCommand(
            toUpdate.Id, "Erik", null, null,
            baseTime.AddMinutes(15), baseTime.AddMinutes(45), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(result.Conflicts);
        Assert.Equal("Jane Doe", result.Conflicts[0].CustomerName);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenNewTimeOverlapsOnlyItself()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var toUpdate = new Appointment { Id = Guid.NewGuid(), CustomerName = "Jane Doe", Start = baseTime, End = baseTime.AddMinutes(30) };

        _repository.Setup(r => r.GetByIdAsync(toUpdate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toUpdate);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { toUpdate });

        // Same appointment, slightly different notes - same time range,
        // must not conflict with itself
        var command = new UpdateAppointmentCommand(
            toUpdate.Id, "Jane Doe", null, null,
            baseTime, baseTime.AddMinutes(30), "Updated notes");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Updated notes", result.Appointment!.Notes);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}