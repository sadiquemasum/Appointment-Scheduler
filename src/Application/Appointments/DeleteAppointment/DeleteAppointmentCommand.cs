using MediatR;

namespace Application.Appointments.DeleteAppointment;

public record DeleteAppointmentCommand(Guid Id) : IRequest<bool>;
