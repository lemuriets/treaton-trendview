namespace LogDecoder.Parser.Data;

public class LogSessionsSorted
{
    private readonly SortedList<DateTime, LogSession> _sessions = [];

    public int TotalSeconds { get; private set; }
    public int Count => _sessions.Count;
    public LogSession this[int index] => _sessions.GetValueAtIndex(index);
    
    public void Add(LogSession session)
    {
        var key = session.Start;
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
            if (session.Start > timeRange.End)
            {
                break;
            }
            if (session.End >= timeRange.Start && session.Start <= timeRange.End)
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
        if (prevSession.End > session.Start)
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
        if (nextSession.Start < session.End)
        {
            throw new ArgumentException("Session overlaps next session");
        }
    }
}

public readonly struct LogSession
{
    public LogSession(
        TimeRange timeRange,
        IReadOnlyList<IndexEntry> indexes,
        long? physicalEndOffsetInLastFile = null)
    {
        if (indexes.Count == 0)
        {
            throw new ArgumentException("Indexes cannot be empty");
        }

        TimeRange = timeRange;
        Indexes = indexes;
        Filenames = CollectFilenames(indexes);
        PhysicalEndOffsetInLastFile = physicalEndOffsetInLastFile;
    }

    public TimeRange TimeRange { get; }
    public IReadOnlyList<IndexEntry> Indexes { get; }
    public IReadOnlyList<string> Filenames { get; }
    public long? PhysicalEndOffsetInLastFile { get; }
    public int TotalSeconds => (int)(TimeRange.Duration).TotalSeconds;
    public DateTime Start => TimeRange.Start;
    public DateTime End => TimeRange.End;
    public long StartOffset => Indexes[0].Offset;
    public long EndOffset => Indexes[^1].Offset;

    private static IReadOnlyList<string> CollectFilenames(IReadOnlyList<IndexEntry> indexes)
    {
        var result = new List<string>();
        string? last = null;
        foreach (var index in indexes)
        {
            if (index.Filename == last)
            {
                continue;
            }
            result.Add(index.Filename);
            last = index.Filename;
        }
        return result;
    }
    
    public bool Contains(DateTime target)
    {
        return target >= Start && target <= End;
    }

    public IndexEntry? FindLastBeforeIndex(DateTime target)
    {
        IndexEntry? result = null;

        foreach (var index in Indexes)
        {
            if (index.Time > target)
            {
                break;
            }
            result = index;
        }
        return result;
    }
}

internal readonly struct OffsetRange(string File, long Start, long End);