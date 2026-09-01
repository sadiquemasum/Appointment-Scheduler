using System.Net.Http.Json;
using Application.Common;

namespace Infrastructure.ExternalServices;

public class ExternalCalendarClient(HttpClient httpClient) : IExternalCalendarClient
{
    public async Task<List<ExternalCalendarEvent>> GetEventsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<List<ExternalEventResponse>>(
            "/api/external/events", cancellationToken);

        return response?
            .Select(e => new ExternalCalendarEvent(e.Id, e.Summary, e.Start, e.End, null))
            .ToList() ?? [];
    }

    private record ExternalEventResponse(string Id, string Summary, DateTimeOffset Start, DateTimeOffset End);
}