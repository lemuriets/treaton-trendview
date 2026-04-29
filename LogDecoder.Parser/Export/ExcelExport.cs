using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using LogDecoder.Helpers;
using Microsoft.Extensions.Logging;

namespace LogDecoder.Parser.Export;

public class ExcelExport : IExcelExport
{
    public ExcelExport(ILogger logger, LogParser logParser)
    {
        _logger = logger;
        _logParser = logParser;
    }

    private readonly ILogger _logger;
    private readonly LogParser _logParser;
    
    public void ToExcel(
        string logsFolder,
        string outputFolder,
        IReadOnlySet<int> filterIds,
        DateTime start,
        DateTime end,
        PackageTechStatus[] techStatusesToParse,
        bool ignoreDuplicates = false,
        bool skipConsecutiveSynchroPackages = false)
    {
        var excelFilePath = Path.Combine(outputFolder, $"Errors Log {DateTime.Now:dd.MM.yyyy HH-mm-ss}.xlsx");
        _logger.LogInformation("Exporting data from: {LogsFolder}. To: {ExcelFilePath}. Ids: [{Ids}]",
            logsFolder,
            excelFilePath,
            string.Join(',', filterIds));
        using var excelSession = new ExcelSession(excelFilePath);
        using var excel = new ExcelHelper(excelSession.Package);

        const string worksheetName = "Errors Log";
        excel.GetOrCreateWorksheet(worksheetName);

        var rowCounter = 0;
        List<string> prevMessages = [];
        ICanPackageParsed? prevPackage = null;
        foreach (var package in _logParser.GetPackages(filterIds, start, end))
        {
            if (skipConsecutiveSynchroPackages &&
                prevPackage != null &&
                prevPackage.Id == package.Id &&
                package.Id == IdSynchro.Id)
            {
                continue;
            }
            var packageData = package.ParseData();
            if (packageData is null)
            {
                continue;
            }
            var packageMessages = packageData.Value.Messages;
            var packageNumericData = packageData.Value.NumericData;
            if (packageMessages.Count == 0 && packageNumericData.Count == 0)
            {
                continue;
            }
            if (ignoreDuplicates && prevMessages.SequenceEqual(packageMessages) && packageNumericData.Count == 0)
            {
                continue;
            }
            
            excel.AddRow(worksheetName, BuildRow(package, packageMessages, packageNumericData));
            
            rowCounter++;
            prevPackage = package;
            prevMessages = packageMessages;
        }
        Console.WriteLine($"Added {rowCounter} rows to excel.");
    }

    private List<object> BuildRow(ICanPackageParsed package, IEnumerable<string> messages, IEnumerable<NumericDataItem> data)
    {
        var row = new List<object>
        {
            package.Id.ToString(),
            package.Name,
            string.Join('\n', messages),
        };
        row.AddRange(data.Select(x => (object)x.Value));
        
        return row;
    }
}