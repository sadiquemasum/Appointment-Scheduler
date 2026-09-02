using Application.Appointments.GetAppointments;
using Application.Common;
using Domain.Entities;
using Moq;

namespace UnitTests;

public class GetAppointmentsHandlerTests
{
    private readonly Mock<IAppointmentRepository> _repository = new();

    private GetAppointmentsHandler CreateHandler() => new(_repository.Object);

    [Fact]
    public async Task Handle_ReturnsAllAppointments_WhenNoFiltersProvided()
    {
        var appointments = new List<Appointment>
        {
            new() { Id = Guid.NewGuid(), CustomerName = "A", Start = DateTimeOffset.UtcNow, End = DateTimeOffset.UtcNow.AddMinutes(30) },
            new() { Id = Guid.NewGuid(), CustomerName = "B", Start = DateTimeOffset.UtcNow.AddDays(5), End = DateTimeOffset.UtcNow.AddDays(5).AddMinutes(30) }
        };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(appointments);

        var result = await CreateHandler().Handle(new GetAppointmentsQuery(null, null), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_ExcludesAppointments_OutsideDateRange()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var inRange = new Appointment { Id = Guid.NewGuid(), CustomerName = "In Range", Start = baseTime, End = baseTime.AddMinutes(30) };
        var outOfRange = new Appointment { Id = Guid.NewGuid(), CustomerName = "Out of Range", Start = baseTime.AddDays(10), End = baseTime.AddDays(10).AddMinutes(30) };

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { inRange, outOfRange });

        var result = await CreateHandler().Handle(
            new GetAppointmentsQuery(baseTime.AddDays(-1), baseTime.AddDays(1)), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("In Range", result[0].CustomerName);
    }

    [Fact]
    public async Task Handle_ReturnsResults_OrderedByStartTime()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var later = new Appointment { Id = Guid.NewGuid(), CustomerName = "Later", Start = baseTime.AddHours(2), End = baseTime.AddHours(2).AddMinutes(30) };
        var earlier = new Appointment { Id = Guid.NewGuid(), CustomerName = "Earlier", Start = baseTime, End = baseTime.AddMinutes(30) };

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { later, earlier });

        var result = await CreateHandler().Handle(new GetAppointmentsQuery(null, null), CancellationToken.None);

        Assert.Equal("Earlier", result[0].CustomerName);
        Assert.Equal("Later", result[1].CustomerName);
    }
}
