namespace Domain.ValueObjects;

public class TimeRange
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public TimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("End must be after start.");

        Start = start;
        End = end;
    }

    // Two ranges overlap if one starts before the other ends,
    // in both directions. Touching edges (one ends exactly when
    // the other starts) do NOT count as a conflict — that's a
    // documented assumption worth stating in your README.
    public bool Overlaps(TimeRange other)
        => Start < other.End && other.Start < End;
}