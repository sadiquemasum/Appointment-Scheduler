using Domain.Entities;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests;

public class AppointmentRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentRepository _repository;

    public AppointmentRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppointmentsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppointmentsDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new AppointmentRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsAppointment()
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            CustomerName = "Repo Test Customer",
            Start = DateTimeOffset.UtcNow,
            End = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await _repository.AddAsync(appointment, CancellationToken.None);
        await _repository.SaveChangesAsync(CancellationToken.None);

        var all = await _repository.GetAllAsync(CancellationToken.None);
        Assert.Single(all);
        Assert.Equal("Repo Test Customer", all[0].CustomerName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAppointmentDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAppointment_WhenItExists()
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            CustomerName = "Findable Customer",
            Start = DateTimeOffset.UtcNow,
            End = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await _repository.AddAsync(appointment, CancellationToken.None);
        await _repository.SaveChangesAsync(CancellationToken.None);

        var found = await _repository.GetByIdAsync(appointment.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("Findable Customer", found!.CustomerName);
    }

    [Fact]
    public async Task Remove_ThenSaveChanges_DeletesAppointment()
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            CustomerName = "To Be Removed",
            Start = DateTimeOffset.UtcNow,
            End = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await _repository.AddAsync(appointment, CancellationToken.None);
        await _repository.SaveChangesAsync(CancellationToken.None);

        _repository.Remove(appointment);
        await _repository.SaveChangesAsync(CancellationToken.None);

        var all = await _repository.GetAllAsync(CancellationToken.None);
        Assert.Empty(all);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
