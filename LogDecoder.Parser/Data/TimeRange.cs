namespace LogDecoder.Parser.Data;

public readonly struct TimeRange
{
    public TimeRange(DateTime start, DateTime end)
    {
        if (start >= end)
        {
            throw new ArgumentException("from must be < to");
        }

        Start = start;
        End = end;
    }
    
    public DateTime Start { get; }
    public DateTime End { get; }
    public TimeSpan Duration => End - Start; 

    public bool Contains(DateTime t)
    {
        return t >= Start && t < End;
    }
}
