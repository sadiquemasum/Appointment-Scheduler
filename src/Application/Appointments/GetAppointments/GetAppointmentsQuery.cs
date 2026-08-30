using Domain.Entities;
using MediatR;

namespace Application.Appointments.GetAppointments;

public record GetAppointmentsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<List<Appointment>>;