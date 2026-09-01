namespace Application.Common;

public interface IExternalCalendarClient
{
    Task<List<ExternalCalendarEvent>> GetEventsAsync(CancellationToken cancellationToken);
}