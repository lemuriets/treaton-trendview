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
    private List<Index> _indexes;

    private string _indexFilePath;

    public Index? FirstIndex;
    public Index? LastIndex;

    public IndexParser(string indexFile)
    {
        _indexFilePath = indexFile;
    }

    public void Load()
    {
        _indexes = LoadIndex(_indexFilePath);

        if (_indexes.Count > 0)
        {
            FirstIndex = _indexes[0];
            LastIndex = _indexes[^1];
        }
    }
    
    private List<Index> LoadIndex(string indexFile)
    {
        if (!File.Exists(indexFile))
        {
            throw new DirectoryNotFoundException($"Specified index file was not found '{indexFile}'");
        }
        Console.WriteLine($"Loading index file {indexFile}");

        var result = new List<Index>();
        var lines = File.ReadAllLines(indexFile);
        foreach (var line in lines)
        {
            var index = ParseLine(line);
            result.Add(index);
        }
        return result;
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