using MediatR;

namespace Application.Appointments.CheckConflict;

public record CheckConflictQuery(
    DateTimeOffset Start,
    DateTimeOffset End,
    Guid? ExcludeId) : IRequest<CheckConflictResult>;

public class CheckConflictResult
{
    public bool HasConflict { get; init; }
    public List<ConflictingAppointment> Conflicts { get; init; } = [];
}

public record ConflictingAppointment(Guid Id, string CustomerName, DateTimeOffset Start, DateTimeOffset End);
