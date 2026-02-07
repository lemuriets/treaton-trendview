using System.Collections;
using System.Text.RegularExpressions;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data;
using LogDecoder.Parser.Contracts;
using Index = System.Index;

namespace LogDecoder.Parser;

public partial class LogParser : ILogParser
{
    public LogParser(string logsFolder)
    {
        _logsFolder = logsFolder;
        _indexFolder = Path.Combine(logsFolder, "index");
        _logFilesSorted = FindBinFilesSorted(logsFolder);
        
        Directory.CreateDirectory(_indexFolder);

        foreach (var file in _logFilesSorted)
        {
            _scanners[file] = new LogFileScanner(file);
        }
    }

    public event Action? StartIndex;
    public event Action? FinishIndex;

    private readonly string _logsFolder;
    private readonly string _indexFolder;
    private readonly string[] _logFilesSorted;
    private readonly IndexParser _indexParser = new IndexParser();
    private readonly SortedDictionary<string, LogFileScanner> _scanners = new();
    private readonly List<Session> _sessions = [];
    
    public readonly HashSet<int> IdsAll = [
        IdSynchro.Id, IdWaveCivl.Id, IdStatusPwr.Id, IdMComplCivl.Id, IdMLeakCivl.Id, 
        IdMMvCivl.Id, IdMPeepCivl.Id, IdMPipCivl.Id, IdMRbCivl.Id, IdMTexpCivl.Id,
        IdMTinspCivl.Id, IdMVexpCivl.Id, IdMVinspCivl.Id, IdStatusScm.Id, IdStatusMix.Id,
        IdStatusCivl.Id, IdStatusMotor.Id, IdStatusCapno1.Id, IdStatusCapno2.Id, IdStatusSpo.Id,
        IdStatusSpoV21.Id, IdStatusSpoV22.Id
    ];
    
    public void CreateAndLoadAllIndexes()
    {
        var indexFiles = new List<string>();
        StartIndex?.Invoke();
        foreach (var file in _logFilesSorted)
        {
            indexFiles.Add(IndexBuilder.CreateIndexFile(file, _indexFolder));
        }
        _indexParser.LoadAll(indexFiles.ToArray());
        FinishIndex?.Invoke();
    }
    
    public IEnumerable<ICanPackageParsed> GetPackagesForTimeSpan(HashSet<int> filterIds, DateTime start, DateTime end)
    {
        foreach (var scanner in _scanners.Values)
        {
            var startBuffer = _indexParser.FindNearestBufferByDateTime(start);
            var endBuffer = _indexParser.FindNearestBufferByDateTime(end);
            var buffersCount = endBuffer - startBuffer;
            foreach (var (_, package) in scanner.ExtractAllPackages(filterIds, startBuffer, buffersCount))
            {
                var parsedPackage = CanPackageFactory.Create(package);
                if (parsedPackage.Id == 0)
                {
                    continue;
                }
                yield return parsedPackage;
            }
        }
    }

    public bool IsDateTimeExists(DateTime target)
    {
        return _indexParser.IsDateTimeExists(target);
    }
    
    public DateTime? GetStartDatetime()
    {
        return _indexParser.FirstTime;
    }

    public DateTime? GetLastDatetime()
    {
        return _indexParser.LastTime;
    }

    public static string[] FindBinFilesSorted(string folder)
    {
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"The specified folder with logs was not found '{folder}'");
        }
        var pattern = FilenameTemplateRegex();
        return Directory
            .GetFileSystemEntries(folder)
            .Where(entry => pattern.IsMatch(Path.GetFileName(entry)))
            .OrderBy(Path.GetFileName)
            .Select(Path.GetFullPath)
            .ToArray();
    }

    [GeneratedRegex(@"^[0-1][0-9]$")]
    private static partial Regex FilenameTemplateRegex();
}