using Application.Common;
using Domain.Entities;
using MediatR;

namespace Application.Appointments.GetAppointments;

public class GetAppointmentsHandler(IAppointmentRepository repository)
    : IRequestHandler<GetAppointmentsQuery, List<Appointment>>
{
    public async Task<List<Appointment>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await repository.GetAllAsync(cancellationToken);

        return appointments
            .Where(a => request.From is null || a.End > request.From)
            .Where(a => request.To is null || a.Start < request.To)
            .OrderBy(a => a.Start)
            .ToList();
    }
}