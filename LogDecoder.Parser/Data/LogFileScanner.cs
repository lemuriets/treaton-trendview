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
        _indexParser = new IndexParser(_indexFile);
    }
    
    private readonly BufferReader _bufferReader;
    private readonly BufferParser _bufferParser;
    private readonly IndexParser _indexParser;
    private readonly string _file;
    private readonly string _indexFolder;
    private readonly string _indexFile;
    
    public void CreateIndexFileIfNotExists()
    {
        if (!File.Exists(_indexFile))
        {
            IndexBuilder.CreateIndexFile(_file, _indexFolder);
        }
        _indexParser.Load();
    }

    private string GetIndexFilePath(string sourceFile, string indexFolder)
    {
        var filename = Path.GetFileName(sourceFile);
        return Path.Combine(indexFolder, $"{filename}.txt");
    }
    
    public DateTime? GetStartDatetime()
    {
        return _indexParser.FirstIndex?.Time;
    }

    public DateTime? GetLastDatetime()
    {
        return _indexParser.LastIndex?.Time;
    }
    
    public bool IsDateTimeExists(DateTime target)
    {
        return _indexParser.FindBufferByDateTime(target) != 1;
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
    
    public IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, DateTime from, DateTime to)
    {
        var prevHrc = 0;
        var currentDateTime = from;
        foreach (var package in ExtractAllPackages(filterIds, from))
        {
            var hrcDelta = CanUtils.CalcHrcDelta(prevHrc, package.Hrc);
            currentDateTime = currentDateTime.AddMicroseconds(hrcDelta);

            if (currentDateTime > to)
            {
                yield break;
            }
            prevHrc = package.Hrc;
            yield return package;
        }
    }
    
    public IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, DateTime from)
    {
        var startBufferNum = _indexParser.FindBufferByDateTime(from);
        if (startBufferNum == -1)
        {
            throw new InvalidOperationException($"Datetime <{from}> does not exists in file {_file}.");
        }
        
        foreach (var package in ExtractAllPackages(filterIds, startBufferNum, 0))
        {
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