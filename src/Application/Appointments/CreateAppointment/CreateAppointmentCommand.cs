using MediatR;

namespace Application.Appointments.CreateAppointment;

public record CreateAppointmentCommand(
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Notes) : IRequest<CreateAppointmentResult>;
