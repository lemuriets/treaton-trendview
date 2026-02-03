using System.Runtime.CompilerServices;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

internal class LogFileScanner: ILogFileScanner
{
    public LogFileScanner(string file, string indexFolder)
    {
        _file = file;
        _indexFolder = indexFolder;
        _indexFile = GetIndexFilePath(file, indexFolder);
        _bufferReader = new BufferReader();
        _bufferParser = new BufferParser();
        _indexReader = new IndexReader(_indexFile);
    }
    
    private readonly BufferReader _bufferReader;
    private readonly BufferParser _bufferParser;
    private readonly IndexReader _indexReader;
    private readonly string _file;
    private readonly string _indexFolder;
    private readonly string _indexFile;
    
    public void CreateIndexFileIfNotExists()
    {
        if (!File.Exists(_indexFile))
        {
            IndexBuilder.CreateIndexFile(_file, _indexFolder);
        }
        _indexReader.Load();
    }

    private string GetIndexFilePath(string sourceFile, string indexFolder)
    {
        var filename = Path.GetFileName(sourceFile);
        return Path.Combine(indexFolder, $"{filename}.txt");
    }
    
    public DateTime? GetStartDatetime()
    {
        return _indexReader.FirstIndex?.Time;
    }

    public DateTime? GetLastDatetime()
    {
        return _indexReader.LastIndex?.Time;
    }
    
    public ICanPackageParsed? GetFirstValidPackage(int id)
    {
        return ExtractAllPackages([id])
            .FirstOrDefault(pkg => pkg.Data.Length != 0);
    }
    
    public IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds)
    {
        foreach (var package in ExtractAllPackages(filterIds, 0, 0))
        {
            yield return package;
        }
    }
    
    public IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, DateTime from)
    {
        var startBufferNum = _indexReader.FindBufferByDateTime(from);
        if (startBufferNum == -1)
        {
            throw new InvalidOperationException($"Datetime <{from}> does not exists in file {_file}.");
        }
        
        foreach (var package in ExtractAllPackages(filterIds, startBufferNum, 0))
        {
            yield return package;
        }
    }
    
    public IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, DateTime from, DateTime to)
    {
        var startBufferNum = _indexReader.FindBufferByDateTime(from);
        var endBufferNum = _indexReader.FindNearestBufferByDateTime(to);

        if (startBufferNum == -1 || endBufferNum == -1 || startBufferNum > endBufferNum)
        {
            Console.WriteLine($"{startBufferNum} {endBufferNum}");
            throw new InvalidOperationException($"Invalid datetime range <{from} - {to}> OR does not exists in file {_file}.");
        }

        var buffersToRead = endBufferNum - startBufferNum + 1;
        foreach (var package in ExtractAllPackages(filterIds, from, buffersToRead))
        {
            yield return package;
        }
    }
    
    public IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, DateTime from, int countSec)
    {
        var to = from.AddSeconds(countSec);
        var prevHrc = 0;
        var currentDateTime = from;
        foreach (var package in ExtractAllPackages(filterIds, from))
        {
            var hrcDelta = CanUtils.CalcHrcDelta(prevHrc, package.Hrc);
            currentDateTime = currentDateTime.AddMicroseconds(hrcDelta);

            if (currentDateTime > to)
            {
                Console.WriteLine(currentDateTime);
                yield break;
            }
            prevHrc = package.Hrc;
            yield return package;
        }
    }
    
    private IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, int offset, int countBuffers)
    {
        foreach (var buffer in _bufferReader.Read(_file, offset, countBuffers))
        {
            foreach (var package in _bufferParser.GetPackagesFromBuffer(buffer, filterIds))
            {
                yield return package;
            }
        }
    }
}