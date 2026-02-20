using System.Globalization;
using LogDecoder.CAN;
using LogDecoder.Parser.Data.Contracts;

namespace LogDecoder.Parser.Data;

public readonly struct IndexEntry(string filename, int bufNum, DateTime time)
{
    public readonly string Filename = filename;
    public readonly int BufferNumber = bufNum;
    public readonly DateTime Time = time;
}

internal class IndexParser : IIndexParser
{
    private List<IndexEntry> _indexes = [];
    private LogSessionsSequence _sessions = new LogSessionsSequence();
    public LogSessionsSequence Sessions => _sessions;

    public DateTime? FirstTime { get; private set; }
    public DateTime? LastTime { get; private set; }
    
    public bool IsDateTimeExists(DateTime target)
    {
        return _sessions.Contains(target);
    }

    public void LoadAll(string[] indexFiles)
    {
        _indexes.Clear();
        foreach (var file in indexFiles)
        {
            _indexes.AddRange(LoadIndexFile(file));
        }

        if (_indexes.Count == 0)
        {
            Console.WriteLine("[WARN] The list of indexes is empty");
            return;
        }

        FirstTime = _indexes[0].Time;
        LastTime = _indexes[^1].Time;
        
        FillSessions(_indexes, _sessions);
        Console.WriteLine($"[INFO] Created indexes. Count: {_indexes.Count}");
    }

    private void FillSessions(List<IndexEntry> indexes, LogSessionsSequence sessions)
    {
        sessions.Clear();
        var timeSpanStart = indexes[0].Time;
        var startBuffer = indexes[0].BufferNumber;
        for (var i = 0; i < indexes.Count - 1; i++)
        {
            var index1 = indexes[i];
            var index2 = indexes[i + 1];

            var timeDiff = (index2.Time - index1.Time).Duration();
            var minTimeDiff = TimeSpan.FromSeconds(Config.MinSessionIntervalSeconds);
            if (timeDiff <= minTimeDiff)
            {
                continue;
            }
            sessions.Add(new LogSession(startBuffer, index1.BufferNumber, new TimeRange(timeSpanStart, index1.Time)));
            timeSpanStart = index2.Time;
            startBuffer = index2.BufferNumber;
        }
        sessions.Add(new LogSession(startBuffer, indexes[^1].BufferNumber, new TimeRange(timeSpanStart, indexes[^1].Time)));
        Console.WriteLine($"[INFO] Created sessions. Count: {sessions.Count}");
    }
    
    public IndexEntry? FindFloor(DateTime target)
    {
        var left = 0;
        var right = _indexes.Count - 1;
        IndexEntry? result = null;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var current = _indexes[mid];

            if (current.Time == target)
            {
                return current;
            }

            if (current.Time < target)
            {
                result = current;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        return result;
    }
    
    private List<IndexEntry> LoadIndexFile(string indexFile)
    {
        if (!File.Exists(indexFile))
        {
            throw new DirectoryNotFoundException($"Specified index file was not found '{indexFile}'");
        }
        Console.WriteLine($"Loading index file {indexFile}");

        var filename = Path.GetFileNameWithoutExtension(indexFile);
        var result = new List<IndexEntry>();
        
        foreach (var line in File.ReadLines(indexFile))
        {
            var (bufNum, dt) = ParseLine(line);
            result.Add(new IndexEntry(filename, bufNum, dt));
        }
        return result;
    }

    private (int, DateTime) ParseLine(string line)
    {
        var spaceIndex = line.IndexOf(' ');
        var strBufNum = line.AsSpan(0, spaceIndex);
        var strTime = line.AsSpan(spaceIndex + 1);

        var bufNum = int.Parse(strBufNum);
        var time = DateTime.ParseExact(strTime, CanConfig.TimeFormat, CultureInfo.InvariantCulture);

        return (bufNum, time);
    }
}