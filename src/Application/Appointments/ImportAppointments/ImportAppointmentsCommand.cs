using MediatR;

namespace Application.Appointments.ImportAppointments;

public record ImportAppointmentsCommand() : IRequest<ImportAppointmentsResult>;

public class ImportAppointmentsResult
{
    public int Imported { get; init; }
    public int SkippedDuplicate { get; init; }
    public int SkippedConflict { get; init; }
    public List<string> ConflictDetails { get; init; } = [];
}
