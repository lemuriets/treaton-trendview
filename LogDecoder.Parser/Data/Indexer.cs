using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data.Contracts;

namespace LogDecoder.Parser.Data;

public readonly struct Index(int bufNum, string timestamp)
{
    public readonly int BufferNumber = bufNum;
    public readonly string Timestamp = timestamp;
}

public class LogFileIndex
{
    public required List<Index> Entries { get; init; } = new();
}

internal class IndexBuilder
{
    private readonly IBufferReader _bufferReader = new BufferReader();
    private readonly IBufferParser _bufferParser = new BufferParser();
    
    public LogFileIndex Build(string file)
    {
        Console.WriteLine($"Creating index for: {file}");

        var entries = new List<Index>();
        var datetimeSet = new HashSet<string>();
        var bufNum = 0;
        foreach (var buffer in _bufferReader.Read(file))
        {
            foreach (var package in _bufferParser.GetPackagesFromBuffer(buffer, [IdSynchro.Id]))
            {
                var packageData = package.ParseData();
                if (packageData is null)
                {
                    continue;
                }
                var dt = packageData.Value.Messages[0];
                if (datetimeSet.Add(dt))
                {
                    entries.Add(new Index(bufNum, dt));
                }
            }
            bufNum += 1;
        }

        return new LogFileIndex { Entries = entries };
    }
}

internal class Indexer : IIndexer
{
    private readonly IBufferReader _bufferReader = new BufferReader();
    private readonly IBufferParser _bufferParser = new BufferParser();
    
    private readonly Dictionary<string, string[]> _indexes = new();

    public void CreateIndexFile(string file, string saveTo)
    {
        Console.WriteLine($"Creating index for: {file}");

        var baseFilename = Path.GetFileName(file);
        var indexFile = Path.Combine(saveTo, baseFilename + ".txt");
        var lines = CreateIndex(file);
        _indexes[indexFile] = lines.ToArray();
        
        if (lines.Length != 0)
        {
            File.WriteAllLines(indexFile, lines);
        }
    }
    
    public int FindBufferByDateTime(string indexFile, DateTime target)
    {
        var lines = GetIndex(indexFile);
        var left = 0;
        var right = lines.Length - 1;
        
        while (left <= right)
        {
            // (left + right) / 2
            var mid = left + (right - left) / 2;
            var (bufNum, dt) = ParseLine(lines[mid]);
            if (dt == target)
            {
                return bufNum;
            }
            if (dt < target)
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

    public int FindNearestBufferByDateTime(string indexFile, DateTime target)
    {
        var lines = GetIndex(indexFile);
        if (lines.Length == 0)
        {
            return -1;
        }
        var lastBufNum = 0;
        foreach (var line in lines)
        {
            var (bufNum, dt) = ParseLine(line);
            if (dt > target)
            {
                break;
            }
            lastBufNum = bufNum;
        }
        return lastBufNum;
    }

    public DateTime? GetLastDatetime(string indexFile)
    {
        var index = GetIndex(indexFile);
        if (index.Length == 0)
        {
            return null;
        }

        for (var i = index.Length - 1; i >= 0; i--)
        {
            var lastLine = index[i];
            if (lastLine == "")
            {
                continue;
            }
            return ParseLine(lastLine).Item2;
        }
        return null;
    }
    
    public string[] GetIndex(string indexFile)
    {
        if (_indexes.TryGetValue(indexFile, out var lines))
        {
            return lines;
        }
        return [];
    }

    private (int, DateTime) ParseLine(string line)
    {
        var midValueSplit = line.Split();
        var strDateTime = string.Join(' ', midValueSplit[1..]);
        var bufNum = int.Parse(midValueSplit[0]);
        var dt = DateTime.Parse(strDateTime);
        return (bufNum, dt);
    }
    
    public void Load(string indexFile)
    {
        if (!File.Exists(indexFile))
        {
            throw new DirectoryNotFoundException($"The specified index file was not found '{indexFile}'");
        }

        Console.WriteLine($"Loading index file {indexFile}");
        _indexes[indexFile] = File.ReadAllLines(indexFile);
    }
    
    private string[] CreateIndex(string sourceFile)
    {
        var lines = new List<string>();
        var datetimeSet = new HashSet<string>();

        var bufNum = 0;
        foreach (var buffer in _bufferReader.Read(sourceFile, 0, 0))
        {
            foreach (var package in _bufferParser.GetPackagesFromBuffer(buffer, [IdSynchro.Id]))
            {
                var packageData = package.ParseData();
                if (packageData is null)
                {
                    continue;
                }
                var dt = packageData.Value.Messages[0];
                if (datetimeSet.Add(dt))
                {
                    lines.Add($"{bufNum} {dt}");
                }
            }
            bufNum += 1;
        }
        return lines.ToArray();
    }
}