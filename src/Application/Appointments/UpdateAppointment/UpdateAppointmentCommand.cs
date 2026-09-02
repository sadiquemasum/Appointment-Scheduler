using MediatR;

namespace Application.Appointments.UpdateAppointment;

public record UpdateAppointmentCommand(
    Guid Id,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Notes) : IRequest<UpdateAppointmentResult>;
