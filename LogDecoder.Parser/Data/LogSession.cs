namespace LogDecoder.Parser.Data;

public class LogSessionsSorted
{
    private readonly SortedList<DateTime, LogSession> _sessions = [];

    public int Count => _sessions.Count;
    public LogSession this[int index] => _sessions.GetValueAtIndex(index);

    public void Add(LogSession session)
    {
        var key = session.StartDT;
        var index = GetInsertionIndex(key);

        ValidatePrev(index, session);
        ValidateNext(index, session);

        _sessions.Add(key, session);
    }

    public List<LogSession> GetRange(TimeRange timeRange)
    {
        var result = new List<LogSession>();
        foreach (var (_, session) in _sessions)
        {
            if (session.StartDT > timeRange.End)
            {
                break;
            }
            if (session.EndDT >= timeRange.Start && session.StartDT <= timeRange.End)
            {
                result.Add(session);
            }
        }
        return result;
    }

    public LogSession? GetSessionByTime(DateTime target)
    {
        return _sessions.Values.FirstOrDefault(s => s.Contains(target));
    }

    public bool Contains(DateTime target)
    {
        return _sessions.Values.Any(s => s.Contains(target));
    }

    public void Clear()
    {
        _sessions.Clear();
    }

    private int GetInsertionIndex(DateTime key)
    {
        var keys = _sessions.Keys.ToList();
        var index = keys.BinarySearch(key);
        if (index < 0)
        {
            index = ~index;
        }
        return index;
    }

    private void ValidatePrev(int index, LogSession session)
    {
        if (index == 0)
        {
            return;
        }
        var prevSession = _sessions.GetValueAtIndex(index - 1);
        if (prevSession.EndDT > session.StartDT)
        {
            throw new ArgumentException("Session overlaps previous session");
        }
    }

    private void ValidateNext(int index, LogSession session)
    {
        if (index >= _sessions.Count)
        {
            return;
        }
        var nextSession = _sessions.GetValueAtIndex(index);
        if (nextSession.StartDT < session.EndDT)
        {
            throw new ArgumentException("Session overlaps next session");
        }
    }
}

public readonly struct LogSession
{
    public LogSession(
        long startOffset,
        long? endOffset,
        DateTime startDt,
        DateTime endDt,
        IReadOnlyList<string> filenames)
    {
        if (filenames.Count == 0)
        {
            throw new ArgumentException("Filenames cannot be empty");
        }
        if (startDt > endDt)
        {
            throw new ArgumentException("StartDT must be <= EndDT");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(startOffset);
        if (endOffset is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endOffset), "EndOffset cannot be negative");
        }
        if (filenames.Count == 1 && endOffset is { } singleFileEnd && startOffset > singleFileEnd)
        {
            throw new ArgumentException("StartOffset must be <= EndOffset within a single file");
        }

        StartOffset = startOffset;
        EndOffset = endOffset;
        StartDT = startDt;
        EndDT = endDt;
        Filenames = filenames;
    }

    public long StartOffset { get; }
    public long? EndOffset { get; }
    public DateTime StartDT { get; }
    public DateTime EndDT { get; }
    public IReadOnlyList<string> Filenames { get; }

    public bool Contains(DateTime target)
    {
        return StartDT <= target && target <= EndDT;
    }
}
