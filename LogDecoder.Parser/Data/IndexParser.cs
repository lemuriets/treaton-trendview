using System.Globalization;
using LogDecoder.CAN;
using LogDecoder.Parser.Data.Contracts;
using Microsoft.Extensions.Logging;

namespace LogDecoder.Parser.Data;

public class IndexParser : IIndexParser
{
    public IndexParser(ILogger logger)
    {
        _logger = logger;
    }

    private const int HeaderLinesQuantity = 1;
    private const int MinIntervalBetweenSessionsSeconds = 20;
    private readonly ILogger _logger;
    private readonly List<IndexEntry> _indexes = [];
    private readonly LogSessionsSequence _sessions = new LogSessionsSequence();
    public LogSessionsSequence Sessions => _sessions;

    public DateTime? MinTime { get; private set; }
    public DateTime? MaxTime { get; private set; }
    
    public bool IsDateTimeExists(DateTime target)
    {
        return _sessions.Contains(target);
    }

    public void LoadAll(string[] indexFiles)
    {
        _indexes.Clear();
        _sessions.Clear();
        MinTime = null;
        MaxTime = null;
        foreach (var file in indexFiles)
        {
            _indexes.AddRange(LoadIndexFile(file));
        }

        if (_indexes.Count == 0)
        {
            _logger.LogWarning("The list of indexes is empty");
            return;
        }

        MinTime = _indexes.Min(i => i.Time);
        MaxTime =_indexes.Max(i => i.Time);
        
        _indexes.Sort((x, y) => x.Time.CompareTo(y.Time));
        
        FillSessions(_indexes);
        _logger.LogInformation(
            "Indexes loaded. Count: {Count}. From: [{MinTime}] To: [{MaxTime}]",
            _indexes.Count,
            MinTime,
            MaxTime);
    }

    private void FillSessions(List<IndexEntry> indexes)
    {
        var timeSpanStart = indexes[0].Time;
        var startOffset = indexes[0].Offset;
        for (var i = 0; i < indexes.Count - 1; i++)
        {
            var index1 = indexes[i];
            var index2 = indexes[i + 1];

            var timeDiff = index2.Time - index1.Time;
            if (timeDiff < TimeSpan.Zero)
            {
                throw new InvalidOperationException("Indexes are not sorted by time.");
            }
            var minTimeDiff = TimeSpan.FromSeconds(MinIntervalBetweenSessionsSeconds);
            if (timeDiff <= minTimeDiff)
            {
                continue;
            }

            var session = new LogSession(startOffset, index1.Offset, new TimeRange(timeSpanStart, index1.Time));
            _sessions.TryAdd(session);
            timeSpanStart = index2.Time;
            startOffset = index2.Offset;
        }
        _sessions.TryAdd(new LogSession(startOffset, indexes[^1].Offset, new TimeRange(timeSpanStart, indexes[^1].Time)));
        _logger.LogInformation("Created sessions. Count: {SessionsCount}", _sessions.Count);
    }
    
    public IndexEntry? FindFloor(DateTime target)
    {
        IndexEntry? result = null;

        foreach (var index in _indexes)
        {
            if (index.Time > target)
            {
                break;
            }
            result = index;
        }
        return result;
    }
    
    private List<IndexEntry> LoadIndexFile(string indexFile)
    {
        if (!File.Exists(indexFile))
        {
            throw new FileNotFoundException($"Specified index file was not found '{indexFile}'");
        }
        _logger.LogInformation("Loading index file {IndexFile}", indexFile);

        var filename = Path.GetFileNameWithoutExtension(indexFile);
        var result = new List<IndexEntry>();
        
        foreach (var line in File.ReadLines(indexFile).Skip(HeaderLinesQuantity))
        {
            var (offset, dt) = ParseLine(line);
            if (dt > DateTime.Now)
            {
                continue;
            }
            result.Add(new IndexEntry(filename, offset, dt));
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