using System.Runtime.CompilerServices;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

internal class LogFileScanner: ILogFileScanner
{
    public LogFileScanner(string file, string indexFolder, Indexer indexer)
    {
        _file = file;
        _indexFolder = indexFolder;
        _indexFile = GetIndexFilePath(file, indexFolder);
        _bufferReader = new BufferReader();
        _bufferParser = new BufferParser();
        _indexer = indexer;
    }
    
    private readonly BufferReader _bufferReader;
    private readonly BufferParser _bufferParser;
    private readonly Indexer _indexer;
    private readonly string _file;
    private readonly string _indexFolder;
    private readonly string _indexFile;

    public string[] GetIndex()
    {
        return _indexer.GetIndex(_indexFile);
    }
    
    public void CreateOrLoadIndexFile()
    {
        if (!File.Exists(_indexFile))
        {
            _indexer.CreateIndexFile(_file, _indexFolder);
        }
        else
        {
            _indexer.Load(_indexFile);
        }
    }

    private string GetIndexFilePath(string sourceFile, string indexFolder)
    {
        var filename = Path.GetFileName(sourceFile);
        return Path.Combine(indexFolder, $"{filename}.txt");
    }
    
    public DateTime? GetStartDatetime()
    {
        var synchro = GetFirstValidPackage(IdSynchro.Id);
        var parsedData = synchro?.ParseData();
        if (parsedData is null)
        {
            return null;
        }
        return DateTime.Parse(parsedData.Value.Messages[0]);
    }

    public DateTime? GetLastDatetime()
    {
        return _indexer.GetLastDatetime(_indexFile);
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
        var startBufferNum = _indexer.FindBufferByDateTime(_indexFile, from);
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
        var startBufferNum = _indexer.FindBufferByDateTime(_indexFile, from);
        var endBufferNum = _indexer.FindNearestBufferByDateTime(_indexFile, to);

        if (startBufferNum == -1 || endBufferNum == -1 || startBufferNum > endBufferNum)
        {
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