using Application.Common;
using Domain.Entities;
using Domain.Services;
using MediatR;

namespace Application.Appointments.CreateAppointment;

public class CreateAppointmentHandler(
    IAppointmentRepository repository,
    ConflictChecker conflictChecker) : IRequestHandler<CreateAppointmentCommand, CreateAppointmentResult>
{
    public async Task<CreateAppointmentResult> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var existingAppointments = await repository.GetAllAsync(cancellationToken);

        var conflicts = conflictChecker.FindConflicts(
            existingAppointments,
            request.Start,
            request.End);

        if (conflicts.Count > 0)
        {
            return CreateAppointmentResult.Conflict(conflicts);
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            Start = request.Start,
            End = request.End,
            Notes = request.Notes
        };

        await repository.AddAsync(appointment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return CreateAppointmentResult.Ok(appointment);
    }
}
