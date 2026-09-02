using Application.Common;
using Domain.Services;
using MediatR;

namespace Application.Appointments.CheckConflict;

public class CheckConflictHandler(
    IAppointmentRepository repository,
    ConflictChecker conflictChecker) : IRequestHandler<CheckConflictQuery, CheckConflictResult>
{
    public async Task<CheckConflictResult> Handle(CheckConflictQuery request, CancellationToken cancellationToken)
    {
        var existingAppointments = await repository.GetAllAsync(cancellationToken);

        var conflicts = conflictChecker.FindConflicts(
            existingAppointments,
            request.Start,
            request.End,
            request.ExcludeId);

        return new CheckConflictResult
        {
            HasConflict = conflicts.Count > 0,
            Conflicts = conflicts
                .Select(c => new ConflictingAppointment(c.Id, c.CustomerName, c.Start, c.End))
                .ToList()
        };
    }
}
