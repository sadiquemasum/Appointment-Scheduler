using Application.Common;
using MediatR;

namespace Application.Appointments.DeleteAppointment;

public class DeleteAppointmentHandler(IAppointmentRepository repository)
    : IRequestHandler<DeleteAppointmentCommand, bool>
{
    public async Task<bool> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        repository.Remove(existing);
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
