using Domain.Entities;

namespace Application.Common;

public interface IAppointmentRepository
{
    Task<List<Appointment>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}