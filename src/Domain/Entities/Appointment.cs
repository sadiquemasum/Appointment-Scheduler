using Domain.ValueObjects;

namespace Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string? Notes { get; set; }
    public string? ExternalId { get; set; }

    public TimeRange ToTimeRange() => new(Start, End);
}