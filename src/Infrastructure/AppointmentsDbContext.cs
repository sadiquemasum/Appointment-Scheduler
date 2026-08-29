using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure;

public class AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options) : DbContext(options)
{
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Start).IsRequired();
            entity.Property(a => a.End).IsRequired();
            // The Postgres exclusion constraint for overlap prevention
            // gets added here later as an optional enhancement, documented
            // in the README — not required for SQLite.
        });
    }
}