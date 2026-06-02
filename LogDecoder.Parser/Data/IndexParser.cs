using System.Globalization;
using System.Runtime.CompilerServices;
using LogDecoder.CAN;
using LogDecoder.Parser.Contracts;
using LogDecoder.Parser.Data.Contracts;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("LogDecoder.Parser.Tests")]

namespace LogDecoder.Parser.Data;

public class IndexParser : IIndexParser
{
    public IndexParser(ILogger logger, ILogFilesAggregator filesAggregator)
    {
        _logger = logger;
        _filesAggregator = filesAggregator;
    }

    private const int HeaderLinesQuantity = 1;
    private static readonly TimeSpan MinIntervalBetweenSessions = TimeSpan.FromSeconds(15);
    private readonly ILogger _logger;
    private readonly ILogFilesAggregator _filesAggregator;
    private readonly LogSessionsSorted _sessions = new LogSessionsSorted();
    private readonly Dictionary<DateTime, IReadOnlyList<IndexEntry>> _entriesByStart = new();
    private readonly List<DateTime> _indexTimes = new();
    public LogSessionsSorted Sessions => _sessions;
    public IReadOnlyList<DateTime> IndexTimes => _indexTimes;

    public DateTime? MinTime { get; private set; }
    public DateTime? MaxTime { get; private set; }

    public bool IsDateTimeExists(DateTime target)
    {
        return _sessions.Contains(target);
    }

    public void LoadAll(string[] indexFiles)
    {
        _sessions.Clear();
        _entriesByStart.Clear();
        _indexTimes.Clear();
        MinTime = null;
        MaxTime = null;

        var entries = indexFiles.SelectMany(LoadIndexFile);
        var groups = BuildSessions(entries).ToList();

        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var nextGroup = i + 1 < groups.Count ? groups[i + 1] : null;

            var session = BuildSession(group, nextGroup);
            _sessions.Add(session);
            _entriesByStart[session.StartDT] = group;

            foreach (var entry in group)
            {
                _indexTimes.Add(entry.Time);
            }
        }

        if (_sessions.Count == 0)
        {
            _logger.LogWarning("The list of sessions is empty");
            return;
        }

        MinTime = _sessions[0].StartDT;
        MaxTime = _sessions[^1].EndDT;

        _logger.LogDebug(
            "Sessions loaded. Count: {Count}. From: [{MinTime}] To: [{MaxTime}]",
            _sessions.Count,
            MinTime,
            MaxTime);
    }

    public IndexEntry FindFloor(LogSession session, DateTime target)
    {
        var entries = _entriesByStart[session.StartDT];
        var result = entries[0];
        foreach (var entry in entries)
        {
            if (entry.Time > target)
            {
                break;
            }
            result = entry;
        }
        return result;
    }

    public IndexEntry? FindCeiling(LogSession session, DateTime target)
    {
        var entries = _entriesByStart[session.StartDT];
        foreach (var entry in entries)
        {
            if (entry.Time > target)
            {
                return entry;
            }
        }
        return null;
    }

    internal static IEnumerable<IReadOnlyList<IndexEntry>> BuildSessions(IEnumerable<IndexEntry> entries)
    {
        var current = new List<IndexEntry>();
        IndexEntry? previous = null;

        foreach (var entry in entries)
        {
            if (previous is not null && IsSessionBoundary(previous.Value.Time, entry.Time))
            {
                yield return current;
                current = [];
            }
            current.Add(entry);
            previous = entry;
        }

        if (current.Count > 0)
        {
            yield return current;
        }
    }

    private LogSession BuildSession(IReadOnlyList<IndexEntry> group, IReadOnlyList<IndexEntry>? nextGroup)
    {
        var first = group[0];
        var last = group[^1];

        long? endOffset = nextGroup is not null && nextGroup[0].Filename == last.Filename
            ? nextGroup[0].Offset
            : null;

        var filenames = _filesAggregator.GetWrappedRange(first.Filename, last.Filename);

        return new LogSession(first.Offset, endOffset, first.Time, last.Time, filenames);
    }

    private static bool IsSessionBoundary(DateTime previous, DateTime current)
    {
        return (current - previous).Duration() >= MinIntervalBetweenSessions;
    }

    private List<IndexEntry> LoadIndexFile(string indexFile)
    {
        if (!File.Exists(indexFile))
        {
            throw new FileNotFoundException($"Specified index file was not found '{indexFile}'");
        }
        _logger.LogDebug("Loading index file {IndexFile}", indexFile);

        var lines = File.ReadLines(indexFile);
        var enumerable = lines as string[] ?? lines.ToArray();
        var header = new IndexFileHeader(enumerable.FirstOrDefault());
        var sourceFileName = header.SourceFileName;
        var result = new List<IndexEntry>();

        foreach (var line in enumerable.Skip(HeaderLinesQuantity))
        {
            var (offset, dt) = ParseLine(line);
            if (dt > DateTime.Now)
            {
                _logger.LogWarning("Found incorrect date in {SourceFile}: {Dt}", sourceFileName, dt);
                continue;
            }
            result.Add(new IndexEntry(sourceFileName, offset, dt));
        }
        return result;
    }

    private (long, DateTime) ParseLine(string line)
    {
        var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var strBufNum =         parts[0];
        var strOffsetInBuffer = parts[1];
        var strTime =           parts[2];

        var bufNum = int.Parse(strBufNum);
        var offsetInBuffer = int.Parse(strOffsetInBuffer);
        var time = DateTime.ParseExact(strTime, CanConfig.TimeFormat, CultureInfo.InvariantCulture);

        var offset = (long)bufNum * LogBuffer.BufferWithHeaderSize + offsetInBuffer;

        return (offset, time);
    }
}
