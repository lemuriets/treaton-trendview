using LogDecoder.Parser.Data.Contracts;

namespace LogDecoder.Parser.Data;

public readonly struct Index(int bufNum, DateTime time)
{
    public readonly int BufferNumber = bufNum;
    public readonly DateTime Time = time;

    public override string ToString()
    {
        return $"{BufferNumber} {Time}";
    }
}

internal class IndexParser : IIndexParser
{
    private List<Index> _indexes = [];
    private List<TimeRange> _sessions = [];

    public DateTime? FirstTime { get; private set; }
    public DateTime? LastTime { get; private set; }
    
    public bool IsDateTimeExists(DateTime target)
    {
        return _sessions.Any(s => s.Contains(target));
    }

    public void LoadAll(string[] indexFiles)
    {
        _indexes.Clear();
        foreach (var file in indexFiles)
        {
            _indexes.AddRange(LoadIndex(file));
        }

        if (_indexes.Count == 0)
        {
            Console.WriteLine("[WARN] The list of indexes is empty");
            return;
        }

        FirstTime = _indexes[0].Time;
        LastTime = _indexes[^1].Time;
        
        FillSessions(_sessions);
        Console.WriteLine($"[INFO] Created indexes. Count: {_indexes.Count}");
    }

    private void FillSessions(List<TimeRange> sessions)
    {
        sessions.Clear();
        var timeSpanStart = _indexes[0].Time;
        for (var i = 0; i < _indexes.Count - 1; i++)
        {
            var index1 = _indexes[i];
            var index2 = _indexes[i + 1];

            var timeDiff = (index2.Time - index1.Time).Duration();
            var minTimeDiff = TimeSpan.FromSeconds(Config.MinSessionIntervalSeconds);
            if (timeDiff <= minTimeDiff)
            {
                continue;
            }
            sessions.Add(new TimeRange(timeSpanStart, index1.Time));
            timeSpanStart = index2.Time;
        }
        sessions.Add(new TimeRange(timeSpanStart, _indexes[^1].Time));
        Console.WriteLine($"[INFO] Created sessions. Count: {sessions.Count}");
    }
    
    public int FindBufferByDateTime(DateTime target)
    {
        var left = 0;
        var right = _indexes.Count - 1;
        
        while (left <= right)
        {
            // (left + right) / 2
            var mid = left + (right - left) / 2;
            var index = _indexes[mid];
            if (index.Time == target)
            {
                return index.BufferNumber;
            }
            if (index.Time < target)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        return -1;
    }

    public int FindNearestBufferByDateTime(DateTime target)
    {
        if (_indexes.Count == 0)
        {
            return -1;
        }
        var lastBufNum = -1;
        foreach (var index in _indexes)
        {
            if (index.Time > target)
            {
                break;
            }
            lastBufNum = index.BufferNumber;
        }
        return lastBufNum;
    }
    
    private List<Index> LoadIndex(string indexFile)
    {
        if (!File.Exists(indexFile))
        {
            throw new DirectoryNotFoundException($"Specified index file was not found '{indexFile}'");
        }
        Console.WriteLine($"Loading index file {indexFile}");

        var lines = File.ReadAllLines(indexFile);
        return lines.Select(ParseLine).ToList();
    }

    private Index ParseLine(string line)
    {
        var spaceIndex = line.IndexOf(' ');
        var strBufNum = line.AsSpan(0, spaceIndex);
        var strTime = line.AsSpan(spaceIndex + 1);

        var bufNum = int.Parse(strBufNum);
        var time = DateTime.Parse(strTime);

        return new Index(bufNum, time);
    }
}