using Domain.Entities;

namespace Application.Common;

public interface IAppointmentRepository
{
    Task<List<Appointment>> GetAllAsync(CancellationToken cancellationToken);
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
    void Remove(Appointment appointment);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}