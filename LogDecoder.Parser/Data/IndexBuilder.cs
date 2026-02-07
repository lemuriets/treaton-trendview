using LogDecoder.CAN.Packages;

namespace LogDecoder.Parser.Data;

public static class IndexBuilder
{
    public static string CreateIndexFile(string logFile, string folderToSave, bool rewrite = false)
    {
        Console.WriteLine($"Creating index for: {logFile}");

        var baseFilename = Path.GetFileName(logFile);
        var indexFilePath = Path.Combine(folderToSave, baseFilename + ".txt");
        var indexes = CreateIndex(logFile);

        File.WriteAllLines(indexFilePath, indexes);

        return indexFilePath;
    }

    private static List<string> CreateIndex(string logFile)
    {
        var fileScanner = new LogFileScanner(logFile);
        
        var indexes = new List<string>();
        var datetimeSet = new HashSet<string>();
        foreach (var (bufNum, package) in fileScanner.ExtractAllPackages([IdSynchro.Id]))
        {
            var parsedPackage = CanPackageFactory.Create(package);
            if (parsedPackage.Id == 0)
            {
                continue;
            }
            var packageData = parsedPackage.ParseData();
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