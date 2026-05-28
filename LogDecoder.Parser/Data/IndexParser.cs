using System.Globalization;
using System.Runtime.CompilerServices;
using LogDecoder.CAN;
using LogDecoder.Parser.Data.Contracts;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("LogDecoder.Parser.Tests")]

namespace LogDecoder.Parser.Data;

public class IndexParser : IIndexParser
{
    public IndexParser(ILogger logger)
    {
        _logger = logger;
    }

    private const int HeaderLinesQuantity = 1;
    private static readonly TimeSpan MinIntervalBetweenSessions = TimeSpan.FromSeconds(15);
    private readonly ILogger _logger;
    private readonly LogSessionsSorted _sessions = new LogSessionsSorted();
    public LogSessionsSorted Sessions => _sessions;

    public DateTime? MinTime { get; private set; }
    public DateTime? MaxTime { get; private set; }

    public bool IsDateTimeExists(DateTime target)
    {
        return _sessions.Contains(target);
    }

    public void LoadAll(string[] indexFiles)
    {
        _sessions.Clear();
        MinTime = null;
        MaxTime = null;

        var entries = indexFiles.SelectMany(LoadIndexFile);
        foreach (var session in BuildSessions(entries))
        {
            _sessions.Add(session);
        }

        if (_sessions.Count == 0)
        {
            _logger.LogWarning("The list of sessions is empty");
            return;
        }

        MinTime = _sessions[0].Start;
        MaxTime = _sessions[^1].End;

        _logger.LogDebug(
            "Sessions loaded. Count: {Count}. From: [{MinTime}] To: [{MaxTime}]",
            _sessions.Count,
            MinTime,
            MaxTime);
    }

    public IndexEntry? FindFloor(DateTime target)
    {
        return _sessions
            .GetSessionByTime(target)?
            .FindLastBeforeIndex(target);
    }

    internal static IEnumerable<LogSession> BuildSessions(IEnumerable<IndexEntry> entries)
    {
        var current = new List<IndexEntry>();
        IndexEntry? previous = null;

        foreach (var entry in entries)
        {
            if (previous is not null && IsSessionBoundary(previous.Value.Time, entry.Time))
            {
                long? physicalEnd = entry.Filename == current[^1].Filename ? entry.Offset : null;
                yield return CreateSession(current, physicalEnd);
                current = [];
            }
            current.Add(entry);
            previous = entry;
        }

        if (current.Count > 0)
        {
            yield return CreateSession(current, null);
        }
    }

    private static bool IsSessionBoundary(DateTime previous, DateTime current)
    {
        return (current - previous).Duration() >= MinIntervalBetweenSessions;
    }

    private static LogSession CreateSession(IReadOnlyList<IndexEntry> indexes, long? physicalEndOffsetInLastFile)
    {
        var start = indexes[0].Time;
        var end = indexes[^1].Time;
        if (start >= end)
        {
            end = start.AddSeconds(1);
        }
        return new LogSession(new TimeRange(start, end), indexes, physicalEndOffsetInLastFile);
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
