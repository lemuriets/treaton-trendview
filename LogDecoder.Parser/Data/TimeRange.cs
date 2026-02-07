namespace LogDecoder.Parser.Data;

public readonly struct TimeRange
{
    public TimeRange(DateTime from, DateTime to)
    {
        if (from >= to)
        {
            throw new ArgumentException("from must be < to");
        }

        From = from;
        To = to;
    }
    
    public DateTime From { get; }
    public DateTime To { get; }

    public bool Contains(DateTime t)
    {
        return t >= From && t < To;
    }
}
