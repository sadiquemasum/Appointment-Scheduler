using Application.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AppointmentRepository(AppointmentsDbContext dbContext) : IAppointmentRepository
{
    public async Task<List<Appointment>> GetAllAsync(CancellationToken cancellationToken)
        => await dbContext.Appointments.ToListAsync(cancellationToken);

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken)
        => await dbContext.Appointments.AddAsync(appointment, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await dbContext.SaveChangesAsync(cancellationToken);
}