using Domain.Entities;

namespace Application.Appointments.UpdateAppointment;

public class UpdateAppointmentResult
{
    public bool Success { get; }
    public bool NotFound { get; }
    public Appointment? Appointment { get; }
    public IReadOnlyList<Appointment> Conflicts { get; }

    private UpdateAppointmentResult(bool success, bool notFound, Appointment? appointment, IReadOnlyList<Appointment> conflicts)
    {
        Success = success;
        NotFound = notFound;
        Appointment = appointment;
        Conflicts = conflicts;
    }

    public static UpdateAppointmentResult Ok(Appointment appointment)
        => new(true, false, appointment, Array.Empty<Appointment>());

    public static UpdateAppointmentResult Conflict(IReadOnlyList<Appointment> conflicts)
        => new(false, false, null, conflicts);

    public static UpdateAppointmentResult AppointmentNotFound()
        => new(false, true, null, Array.Empty<Appointment>());
}
