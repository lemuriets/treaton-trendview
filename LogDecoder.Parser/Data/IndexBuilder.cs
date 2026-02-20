using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;

namespace LogDecoder.Parser.Data;

public class IndexBuilder
{
    public IndexBuilder(ICanPackageFactory factory)
    {
        _factory = factory;
    }

    private readonly ICanPackageFactory _factory;
    
    public string CreateIndexFile(string logFile, string folderToSave, bool rewrite = false)
    {
        Console.WriteLine($"Creating index for: {logFile}");

        var baseFilename = Path.GetFileName(logFile);
        var indexFilePath = Path.Combine(folderToSave, baseFilename + ".txt");

        if (!File.Exists(indexFilePath) || rewrite)
        {
            var indexes = CreateIndex(logFile);
            File.WriteAllLines(indexFilePath, indexes);
        }
        return indexFilePath;
    }

    private List<string> CreateIndex(string logFile)
    {
        var fileScanner = new LogFileScanner(logFile);
        
        var indexes = new List<string>();
        var datetimeSet = new HashSet<string>();
        foreach (var (bufNum, package) in fileScanner.GetAllPackagesParsed(_factory, new HashSet<int>{IdSynchro.Id}))
        {
            var packageData = package.ParseData();
            if (packageData is null)
            {
                continue;
            }
            var dt = packageData.Value.Messages[0];
            if (datetimeSet.Add(dt))
            {
                indexes.Add($"{bufNum} {dt}");
            }
        }
        return indexes;
    }
}