using Application.Appointments.CheckConflict;
using Application.Common;
using Domain.Entities;
using Domain.Services;
using Moq;

namespace UnitTests;

public class CheckConflictHandlerTests
{
    private readonly Mock<IAppointmentRepository> _repository = new();
    private readonly ConflictChecker _conflictChecker = new();

    private CheckConflictHandler CreateHandler() => new(_repository.Object, _conflictChecker);

    [Fact]
    public async Task Handle_ReturnsHasConflictTrue_WhenOverlapExists()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existing = new Appointment { Id = Guid.NewGuid(), CustomerName = "Jane Doe", Start = baseTime, End = baseTime.AddMinutes(30) };

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { existing });

        var query = new CheckConflictQuery(baseTime.AddMinutes(15), baseTime.AddMinutes(45), null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.True(result.HasConflict);
        Assert.Single(result.Conflicts);
    }

    [Fact]
    public async Task Handle_ReturnsHasConflictFalse_WhenExcludingOwnAppointment()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existing = new Appointment { Id = Guid.NewGuid(), CustomerName = "Jane Doe", Start = baseTime, End = baseTime.AddMinutes(30) };

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { existing });

        var query = new CheckConflictQuery(baseTime, baseTime.AddMinutes(30), existing.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.False(result.HasConflict);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public async Task Handle_ReturnsHasConflictFalse_WhenSlotIsFree()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());

        var query = new CheckConflictQuery(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        Assert.False(result.HasConflict);
    }
}
