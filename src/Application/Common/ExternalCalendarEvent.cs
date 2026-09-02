namespace Application.Common;

public record ExternalCalendarEvent(
    string ExternalId,
    string CustomerName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Notes);
