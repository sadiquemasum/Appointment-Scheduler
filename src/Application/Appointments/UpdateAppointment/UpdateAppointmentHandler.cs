using Application.Common;
using Domain.Services;
using MediatR;

namespace Application.Appointments.UpdateAppointment;

public class UpdateAppointmentHandler(
    IAppointmentRepository repository,
    ConflictChecker conflictChecker) : IRequestHandler<UpdateAppointmentCommand, UpdateAppointmentResult>
{
    public async Task<UpdateAppointmentResult> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return UpdateAppointmentResult.AppointmentNotFound();
        }

        var allAppointments = await repository.GetAllAsync(cancellationToken);

        var conflicts = conflictChecker.FindConflicts(
            allAppointments,
            request.Start,
            request.End,
            excludeId: request.Id);

        if (conflicts.Count > 0)
        {
            return UpdateAppointmentResult.Conflict(conflicts);
        }

        existing.CustomerName = request.CustomerName;
        existing.CustomerPhone = request.CustomerPhone;
        existing.CustomerEmail = request.CustomerEmail;
        existing.Start = request.Start;
        existing.End = request.End;
        existing.Notes = request.Notes;

        await repository.SaveChangesAsync(cancellationToken);

        return UpdateAppointmentResult.Ok(existing);
    }
}
