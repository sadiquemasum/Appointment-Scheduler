using Domain.Entities;

namespace Domain.Services;

public class ConflictChecker
{
    // Returns the appointments (if any) that conflict with the
    // proposed time range. excludeId lets an update check against
    // everything except itself.
    public IReadOnlyList<Appointment> FindConflicts(
        IEnumerable<Appointment> existingAppointments,
        DateTimeOffset proposedStart,
        DateTimeOffset proposedEnd,
        Guid? excludeId = null)
    {
        var proposed = new Domain.ValueObjects.TimeRange(proposedStart, proposedEnd);

        return existingAppointments
            .Where(a => excludeId is null || a.Id != excludeId)
            .Where(a => a.ToTimeRange().Overlaps(proposed))
            .ToList();
    }
}
