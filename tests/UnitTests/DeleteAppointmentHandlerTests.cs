using Application.Appointments.DeleteAppointment;
using Application.Common;
using Domain.Entities;
using Moq;

namespace UnitTests;

public class DeleteAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _repository = new();

    [Fact]
    public async Task Handle_ReturnsTrue_AndRemoves_WhenAppointmentExists()
    {
        var appointment = new Appointment { Id = Guid.NewGuid(), CustomerName = "Jane Doe" };
        _repository.Setup(r => r.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        var handler = new DeleteAppointmentHandler(_repository.Object);
        var result = await handler.Handle(new DeleteAppointmentCommand(appointment.Id), CancellationToken.None);

        Assert.True(result);
        _repository.Verify(r => r.Remove(appointment), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenAppointmentNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var handler = new DeleteAppointmentHandler(_repository.Object);
        var result = await handler.Handle(new DeleteAppointmentCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
        _repository.Verify(r => r.Remove(It.IsAny<Appointment>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
