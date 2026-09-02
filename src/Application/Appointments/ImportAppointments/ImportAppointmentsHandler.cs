using Application.Common;
using Domain.Entities;
using Domain.Services;
using MediatR;

namespace Application.Appointments.ImportAppointments;

public class ImportAppointmentsHandler(
    IAppointmentRepository repository,
    IExternalCalendarClient externalClient,
    ConflictChecker conflictChecker) : IRequestHandler<ImportAppointmentsCommand, ImportAppointmentsResult>
{
    public async Task<ImportAppointmentsResult> Handle(ImportAppointmentsCommand request, CancellationToken cancellationToken)
    {
        var externalEvents = await externalClient.GetEventsAsync(cancellationToken);
        var existingAppointments = await repository.GetAllAsync(cancellationToken);

        var existingExternalIds = existingAppointments
            .Where(a => a.ExternalId != null)
            .Select(a => a.ExternalId)
            .ToHashSet();

        // Tracks both already-saved appointments AND ones imported earlier
        // in this same batch, so two conflicting external events in one
        // import run correctly flag each other too.
        var workingSet = new List<Appointment>(existingAppointments);

        int imported = 0, skippedDuplicate = 0, skippedConflict = 0;
        var conflictDetails = new List<string>();

        foreach (var evt in externalEvents)
        {
            if (existingExternalIds.Contains(evt.ExternalId))
            {
                skippedDuplicate++;
                continue;
            }

            var conflicts = conflictChecker.FindConflicts(workingSet, evt.Start, evt.End);
            if (conflicts.Count > 0)
            {
                skippedConflict++;
                conflictDetails.Add($"{evt.CustomerName} ({evt.Start:yyyy-MM-dd HH:mm}) conflicts with {conflicts[0].CustomerName}");
                continue;
            }

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                CustomerName = evt.CustomerName,
                Start = evt.Start,
                End = evt.End,
                Notes = evt.Notes,
                ExternalId = evt.ExternalId
            };

            await repository.AddAsync(appointment, cancellationToken);
            workingSet.Add(appointment);
            imported++;
        }

        await repository.SaveChangesAsync(cancellationToken);

        return new ImportAppointmentsResult
        {
            Imported = imported,
            SkippedDuplicate = skippedDuplicate,
            SkippedConflict = skippedConflict,
            ConflictDetails = conflictDetails
        };
    }
}
