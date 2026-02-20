using System.Globalization;
using System.Text.RegularExpressions;
using LogDecoder.Parser.Data;

namespace LogDecoder.Parser;

public sealed class JournalParser
{
    private static readonly Regex LineRegex = new(
        @"^(log_start|log_stop)\s+(\d{2}:\d{2}:\d{2})\s+(\d{2}\.\d{2}\.\d{4})\s+(\d+)\s+(\d+)$",
        RegexOptions.Compiled);

    public LogSessionsSequence Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var events = ParseEvents(lines);

        var ordered = events
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.LineNumber)
            .ToList();

        return BuildSequence(ordered);
    }

    private static List<LogEvent> ParseEvents(IEnumerable<string> lines)
    {
        var result = new List<LogEvent>();
        int lineNumber = 0;

        foreach (var line in lines)
        {
            lineNumber++;

            if (!TryParseLine(line, lineNumber, out var logEvent))
            {
                continue;
            }
            if (logEvent.Timestamp.Year >= DateTime.Now.Year)
            {
                continue;
            }
            result.Add(logEvent);
        }

        return result;
    }

    private static bool TryParseLine(string line, int lineNumber, out LogEvent? logEvent)
    {
        logEvent = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var match = LineRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        var type = match.Groups[1].Value == "log_start"
            ? LogEventType.Start
            : LogEventType.Stop;

        if (!DateTime.TryParseExact(
                $"{match.Groups[3].Value} {match.Groups[2].Value}",
                "dd.MM.yyyy HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            return false;
        }

        var fileName = match.Groups[4].Value;

        if (!int.TryParse(match.Groups[5].Value, out var buffer))
        {
            return false;
        }

        logEvent = new LogEvent(type, timestamp, fileName, buffer, lineNumber);

        return true;
    }

    private static LogSessionsSequence BuildSequence(IReadOnlyList<LogEvent> events)
    {
        var sessionsSequence = new LogSessionsSequence();
        LogEvent? currentStart = null;

        foreach (var e in events)
        {
            switch (e.Type)
            {
                case LogEventType.Start:
                    currentStart = e;
                    break;

                case LogEventType.Stop:
                    if (currentStart == null)
                    {
                        break;
                    }
                    if (e.Timestamp <= currentStart.Timestamp)
                    {
                        break;
                    }

                    var session = new LogSession(currentStart.BufferNumber, e.BufferNumber, new TimeRange(currentStart.Timestamp, e.Timestamp));

                    sessionsSequence.TryAdd(session);

                    currentStart = null;
                    break;
            }
        }
        return sessionsSequence;
    }

    private enum LogEventType
    {
        Start,
        Stop
    }

    private sealed record LogEvent(
        LogEventType Type,
        DateTime Timestamp,
        string FileName,
        int BufferNumber,
        int LineNumber);
}
