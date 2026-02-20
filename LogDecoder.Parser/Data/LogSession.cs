namespace LogDecoder.Parser.Data;

public class LogSessionsSequence
{
    private readonly List<LogSession> _sessions = [];

    public int TotalSeconds { get; private set; }
    public int Count => _sessions.Count;

    public void Add(LogSession session)
    {
        if (_sessions.Count > 0)
        {
            if (_sessions[^1].TimeRange.To > session.TimeRange.From)
            {
                throw new ArgumentException("Previous session ending time must be less than or equal to start of new one");
            }
        }
        _sessions.Add(session);
        TotalSeconds += session.TotalSeconds;
    }

    public bool TryAdd(LogSession session)
    {
        try
        {
            Add(session);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool Contains(DateTime target)
    {
        return _sessions.Any(s => s.Contains(target));
    }
    
    public LogSession? GetSessionByTime(DateTime target)
    {
        return _sessions.FirstOrDefault(s => s.Contains(target));
    }

    public int IndexOf(DateTime target)
    {
        var session = GetSessionByTime(target);
        if (session is null)
        {
            return -1;
        }
        return session.IndexOf(target);
    }

    public void Clear()
    {
        _sessions.Clear();
    }
}

public record LogSession(int StartBufNum, int EndBufNum, TimeRange TimeRange)
{
    public readonly int TotalSeconds = (int)(TimeRange.To - TimeRange.From).TotalSeconds;

    public bool Contains(DateTime target)
    {
        return TimeRange.Contains(target);
    }

    public int IndexOf(DateTime target)
    {
        if (!Contains(target))
        {
            return -1;
        }
        return (int)(target - TimeRange.From).TotalSeconds;
    }
}