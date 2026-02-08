namespace LogDecoder.Parser.Data;

public class LogSessionsSequence()
{
    private readonly List<LogSession> _sessions = [];

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
    }

    public LogSession? GetSessionByTime(DateTime target)
    {
        return _sessions.FirstOrDefault(s => s.Contains(target));
    }
}

public record LogSession(int StartBufNum, int EndBufNum, TimeRange TimeRange)
{
    public int TotalSeconds => (int)(TimeRange.To - TimeRange.From).TotalSeconds;

    public bool Contains(DateTime target)
    {
        return TimeRange.Contains(target);
    }
}