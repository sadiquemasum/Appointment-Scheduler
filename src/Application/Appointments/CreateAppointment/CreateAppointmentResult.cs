using Domain.Entities;

namespace Application.Appointments.CreateAppointment;

public class CreateAppointmentResult
{
    public bool Success { get; }
    public Appointment? Appointment { get; }
    public IReadOnlyList<Appointment> Conflicts { get; }

    private CreateAppointmentResult(bool success, Appointment? appointment, IReadOnlyList<Appointment> conflicts)
    {
        Success = success;
        Appointment = appointment;
        Conflicts = conflicts;
    }

    public static CreateAppointmentResult Ok(Appointment appointment)
        => new(true, appointment, Array.Empty<Appointment>());

    public static CreateAppointmentResult Conflict(IReadOnlyList<Appointment> conflicts)
        => new(false, null, conflicts);
}
