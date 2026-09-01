using Application.Appointments.ImportAppointments;
using Application.Common;
using Domain.Entities;
using Domain.Services;
using Moq;

namespace UnitTests;

public class ImportAppointmentsHandlerTests
{
    private readonly Mock<IAppointmentRepository> _repository = new();
    private readonly Mock<IExternalCalendarClient> _externalClient = new();
    private readonly ConflictChecker _conflictChecker = new();

    private ImportAppointmentsHandler CreateHandler() => new(_repository.Object, _externalClient.Object, _conflictChecker);

    [Fact]
    public async Task Handle_ImportsNewEvents_WhenNoConflictsOrDuplicates()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var events = new List<ExternalCalendarEvent>
        {
            new("ext-001", "Karin Berg", baseTime, baseTime.AddMinutes(30), null),
            new("ext-002", "Oscar Nilsson", baseTime.AddHours(2), baseTime.AddHours(2).AddMinutes(30), null)
        };

        _externalClient.Setup(c => c.GetEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(events);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());

        var result = await CreateHandler().Handle(new ImportAppointmentsCommand(), CancellationToken.None);

        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.SkippedDuplicate);
        Assert.Equal(0, result.SkippedConflict);
        _repository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SkipsAsDuplicate_WhenExternalIdAlreadyImported()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var alreadyImported = new Appointment
        {
            Id = Guid.NewGuid(), CustomerName = "Karin Berg",
            Start = baseTime, End = baseTime.AddMinutes(30), ExternalId = "ext-001"
        };
        var events = new List<ExternalCalendarEvent> { new("ext-001", "Karin Berg", baseTime, baseTime.AddMinutes(30), null) };

        _externalClient.Setup(c => c.GetEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(events);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment> { alreadyImported });

        var result = await CreateHandler().Handle(new ImportAppointmentsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.SkippedDuplicate);
        _repository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SkipsAsConflict_WhenOverlapsExistingAppointment()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existing = new Appointment { Id = Guid.NewGuid(), CustomerName = "Jane Doe", Start = baseTime, End = baseTime.AddMinutes(30) };
        var events = new List<ExternalCalendarEvent> { new("ext-003", "Follow-up - Jane Doe", baseTime, baseTime.AddMinutes(30), null) };

        _externalClient.Setup(c => c.GetEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(events);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment> { existing });

        var result = await CreateHandler().Handle(new ImportAppointmentsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.SkippedConflict);
        Assert.Single(result.ConflictDetails);
        _repository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SkipsSecondEvent_WhenTwoImportedEventsConflictWithEachOtherInSameBatch()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var events = new List<ExternalCalendarEvent>
        {
            new("ext-010", "First Customer", baseTime, baseTime.AddMinutes(30), null),
            new("ext-011", "Second Customer", baseTime.AddMinutes(15), baseTime.AddMinutes(45), null) // overlaps ext-010
        };

        _externalClient.Setup(c => c.GetEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(events);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());

        var result = await CreateHandler().Handle(new ImportAppointmentsCommand(), CancellationToken.None);

        // First event imports cleanly; second conflicts with the first
        // one added earlier in this same batch (tests the workingSet logic)
        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.SkippedConflict);
    }

    [Fact]
    public async Task Handle_ReturnsAllZeros_WhenExternalApiReturnsNoEvents()
    {
        _externalClient.Setup(c => c.GetEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ExternalCalendarEvent>());
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());

        var result = await CreateHandler().Handle(new ImportAppointmentsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Imported);
        Assert.Equal(0, result.SkippedDuplicate);
        Assert.Equal(0, result.SkippedConflict);
        _repository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}