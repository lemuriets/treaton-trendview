using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.Helpers;

namespace LogDecoder.Parser.Export;

public class ExcelExport(LogParser logParser) : IExcelExport
{
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
        Console.WriteLine($"Exporting data from: {logsFolder}. To: {excelFilePath}. Ids: [{string.Join(',', filterIds)}]");
        using var excelSession = new ExcelSession(excelFilePath);
        using var excel = new ExcelHelper(excelSession.Package);

        const string worksheetName = "Errors Log";
        excel.GetOrCreateWorksheet(worksheetName);

        var rowCounter = 0;
        string[] prevMessages = [];
        ICanPackageParsed? prevPackage = null;
        foreach (var package in logParser.GetPackages(filterIds, start, end))
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
            if (packageMessages.Length == 0 && packageNumericData.Length == 0)
            {
                continue;
            }
            if (ignoreDuplicates && prevMessages.SequenceEqual(packageMessages) && packageNumericData.Length == 0)
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