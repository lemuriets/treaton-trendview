using System.Text.RegularExpressions;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data;
using LogDecoder.Parser.Contracts;

namespace LogDecoder.Parser;

public partial class LogParser : ILogParser
{
    public LogParser(string logsFolder)
    {
        _logsFolder = logsFolder;
        _indexFolder = Path.Combine(logsFolder, "index");
        _filesAggrerator = new LogFilesAggregator(_logsFolder, Path.GetFileName, FilenameTemplateRegex());
        _factory = new CanPackageFactory();
        _indexBuilder = new IndexBuilder(_factory);
        _indexParser = new IndexParser();

        IdsAll = _factory.RegisteredIds;
        
        Directory.CreateDirectory(_indexFolder);
    }

    public event Action? StartIndex;
    public event Action? FinishIndex;

    private readonly ICanPackageFactory _factory;
    private readonly string _logsFolder;
    private readonly string _indexFolder;
    private readonly LogFilesAggregator _filesAggrerator;
    private readonly IndexBuilder _indexBuilder;
    private readonly IndexParser _indexParser;
    private readonly Dictionary<string, LogFileScanner> _scanners = new();

    public readonly IReadOnlySet<int> IdsAll;
    
    public bool IsDateTimeExists(DateTime target) => _indexParser.IsDateTimeExists(target);
    public DateTime? GetStartDatetime() => _indexParser.FirstTime;
    public DateTime? GetLastDatetime() => _indexParser.LastTime;
    
    public void CreateOrLoadAllIndexes()
    {
        var indexFiles = new List<string>();
        StartIndex?.Invoke();
        foreach (var file in _filesAggrerator.SortedFiles)
        {
            indexFiles.Add(_indexBuilder.CreateIndexFile(file, _indexFolder));
        }
        _indexParser.LoadAll(indexFiles.ToArray());
        FinishIndex?.Invoke();
    }
    
    public async Task CreateOrLoadAllIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexFiles = new List<string>();

        StartIndex?.Invoke();

        await Task.Run(() =>
        {
            foreach (var file in _filesAggrerator.SortedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                indexFiles.Add(_indexBuilder.CreateIndexFile(file, _indexFolder));
            }
            _indexParser.LoadAll(indexFiles.ToArray());

        }, cancellationToken);

        FinishIndex?.Invoke();
    }
    
    public IEnumerable<ICanPackageParsed> GetPackages(IReadOnlySet<int> filterIds, DateTime start, DateTime end)
    {
        var startIndex = _indexParser.FindFloor(start);
        var endIndex = _indexParser.FindFloor(end);
        if (startIndex is null || endIndex is null)
        {
            throw new Exception($"Unable to find indexes range. ({start} - {end}) -> {startIndex} {endIndex}");
        }
        
        var startFilename = startIndex.Value.Filename;
        var endFilename = endIndex.Value.Filename;
        
        foreach (var file in _filesAggrerator.GetRange(startFilename, endFilename))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            var scanner = GetScanner(file);

            var (offset, count) = ResolveScanRange(filename, startFilename, endFilename, startIndex.Value.BufferNumber, endIndex.Value.BufferNumber);
            foreach (var (_, package) in scanner.GetAllPackagesParsed(_factory, filterIds, offsetBuffers: offset, countBuffers: count))
            {
                yield return package;
            }
        }
    }
    
    private (int offset, int count) ResolveScanRange(string filename, string startFilename, string endFilename, int startBuffer, int endBuffer)
    {
        if (startFilename == endFilename)
        {
            if (filename == startFilename)
            {
                return (startBuffer, endBuffer - startBuffer);
            }
            return (0, 0);
        }
        if (filename == startFilename)
        {
            return (startBuffer, 0);
        }
        if (filename == endFilename)
        {
            return (0, endBuffer);
        }
        return (0, 0);
    }

    
    private LogFileScanner GetScanner(string file)
    {
        if (_scanners.TryGetValue(file, out var scanner))
        {
            return scanner;
        }
        scanner = new LogFileScanner(file);
        _scanners[file] = scanner;
        return scanner;
    }

    [GeneratedRegex(@"^[0-1][0-9]$")]
    private static partial Regex FilenameTemplateRegex();
}