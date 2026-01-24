using System.Text.RegularExpressions;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data;
using LogDecoder.Parser.Data.Contracts;
using LogDecoder.Parser.Export;
using LogDecoder.Parser.Contracts;

namespace LogDecoder.Parser;

public partial class LogParser : ILogParser
{
    public LogParser(string logsFolder)
    {
        _logsFolder = logsFolder;
        var binFilesSorted = FindBinFilesSorted(logsFolder);
        
        _indexFolder = Path.Combine(logsFolder, "index");
        _indexer = new Indexer();
        
        Directory.CreateDirectory(_indexFolder);
        
        foreach (var file in binFilesSorted)
        {
            _scanners[file] = new LogFileScanner(file, _indexFolder, _indexer);
        }
    }

    public event Action? StartIndex;
    public event Action? FinishIndex;

    private readonly string _logsFolder;
    private readonly string _indexFolder;
    private readonly IIndexer _indexer;

    private readonly SortedDictionary<string, ILogFileScanner> _scanners = new();
    
    public readonly HashSet<int> IdsAll = [IdSynchro.Id, IdWaveCivl.Id, IdStatusPwr.Id, IdMComplCivl.Id, IdMLeakCivl.Id, IdMMvCivl.Id, IdMPeepCivl.Id, IdMPipCivl.Id, IdMRbCivl.Id, IdMTexpCivl.Id, IdMTinspCivl.Id, IdMVexpCivl.Id, IdMVinspCivl.Id, IdStatusScm.Id, IdStatusMix.Id, IdStatusCivl.Id, IdStatusMotor.Id, IdStatusCapno1.Id, IdStatusCapno2.Id, IdStatusSpo.Id, IdStatusSpoV21.Id, IdStatusSpoV22.Id];

    public List<string> GetAllIndexes()
    {
        var result = new List<string>();
        foreach (var scanner in _scanners.Values)
        {
            foreach (var line in scanner.GetIndex())
            {
                result.Add(string.Join(' ', line.Split()[1..]));
            }
        }
        return result;
    }
    
    public void CreateOrLoadAllIndexes()
    {
        StartIndex?.Invoke();

        foreach (var scanner in _scanners.Values)
        {
            scanner.CreateOrLoadIndexFile();
        }

        FinishIndex?.Invoke();
    }
    
    public async Task CreateAllIndexesAsync()
    {
        StartIndex?.Invoke();
        var tasks = _scanners.Values
            .Select(scanner => Task.Run(scanner.CreateOrLoadIndexFile));

        await Task.WhenAll(tasks);
        
        FinishIndex?.Invoke();
    }
    
    public List<ICanPackageParsed> GetPackagesForTimeSpan(HashSet<int> filterIds, DateTime start, int lenghtSec)
    {
        List<ICanPackageParsed> packages = [];
        foreach (var scanner in _scanners.Values)
        {
            try
            {
                return scanner.ExtractAllPackages(filterIds, start, lenghtSec).ToList();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"Unable to extract packages for {start} - {lenghtSec} sec");
            }
        }
        return packages;
    }

    public bool IsDateTimeExists(DateTime dt)
    {
        foreach (var VARIABLE in COLLECTION)
        {
            
        }
        return _indexer.FindBufferByDateTime() != -1;
    }
    
    public DateTime? GetStartDatetime()
    {
        return _scanners.Values.FirstOrDefault()?.GetStartDatetime();
    }

    public DateTime? GetLastDatetime()
    {
        foreach (var scanner in _scanners.Values.Reverse())
        {
            var dt = scanner.GetLastDatetime();
            if (dt is not null)
            {
                return dt;
            }
        }
        return null;
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