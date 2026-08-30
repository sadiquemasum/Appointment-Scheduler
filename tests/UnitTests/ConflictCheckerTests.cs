using Domain.Entities;
using Domain.Services;

namespace UnitTests;

public class ConflictCheckerTests
{
    private readonly ConflictChecker _checker = new();

    private static Appointment CreateAppointment(DateTimeOffset start, DateTimeOffset end, Guid? id = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            CustomerName = "Test Customer",
            Start = start,
            End = end
        };

    [Fact]
    public void FindConflicts_ReturnsEmpty_WhenNoExistingAppointments()
    {
        var result = _checker.FindConflicts(
            existingAppointments: Enumerable.Empty<Appointment>(),
            proposedStart: DateTimeOffset.UtcNow,
            proposedEnd: DateTimeOffset.UtcNow.AddMinutes(30));

        Assert.Empty(result);
    }

    [Fact]
    public void FindConflicts_ReturnsConflict_WhenTimesOverlap()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existing = CreateAppointment(baseTime, baseTime.AddMinutes(30));

        // Proposed 10:15–10:45 overlaps the existing 10:00–10:30
        var result = _checker.FindConflicts(
            existingAppointments: new[] { existing },
            proposedStart: baseTime.AddMinutes(15),
            proposedEnd: baseTime.AddMinutes(45));

        Assert.Single(result);
        Assert.Equal(existing.Id, result[0].Id);
    }

    [Fact]
    public void FindConflicts_ReturnsEmpty_WhenAppointmentsAreBackToBack()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existing = CreateAppointment(baseTime, baseTime.AddMinutes(30));

        // Proposed 10:30–11:00 starts exactly when the existing one ends —
        // touching edges are NOT a conflict per our documented assumption
        var result = _checker.FindConflicts(
            existingAppointments: new[] { existing },
            proposedStart: baseTime.AddMinutes(30),
            proposedEnd: baseTime.AddMinutes(60));

        Assert.Empty(result);
    }

    [Fact]
    public void FindConflicts_ExcludesSpecifiedAppointment_ForUpdateScenario()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var existingId = Guid.NewGuid();
        var existing = CreateAppointment(baseTime, baseTime.AddMinutes(30), existingId);

        // Same time range as the existing appointment, but excluding its own id
        // (simulates saving an update without triggering a self-conflict)
        var result = _checker.FindConflicts(
            existingAppointments: new[] { existing },
            proposedStart: baseTime,
            proposedEnd: baseTime.AddMinutes(30),
            excludeId: existingId);

        Assert.Empty(result);
    }

    [Fact]
    public void FindConflicts_ReturnsMultiple_WhenOverlappingMultipleAppointments()
    {
        var baseTime = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var first = CreateAppointment(baseTime, baseTime.AddMinutes(30));
        var second = CreateAppointment(baseTime.AddMinutes(20), baseTime.AddMinutes(50));

        // Proposed 10:10–10:40 overlaps both existing appointments
        var result = _checker.FindConflicts(
            existingAppointments: new[] { first, second },
            proposedStart: baseTime.AddMinutes(10),
            proposedEnd: baseTime.AddMinutes(40));

        Assert.Equal(2, result.Count);
    }
}