using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Helpers;

namespace LogDecoder.Parser.Export;

public class ExcelExport(LogParser logParser) : IExcelExport
{
    public void ToExcel(string logsFolder, string outputFolder, IReadOnlySet<int> filterIds, DateTime start, DateTime end)
    {
        var excelFilePath = Path.Combine(outputFolder, "Errors Log.xlsx");
        Console.WriteLine($"Exporting data from: {logsFolder}. To: {excelFilePath}. Ids: [{string.Join(',', filterIds)}]");
        using var excelSession = new ExcelSession(excelFilePath);
        using var excel = new ExcelHelper(excelSession.Package);

        const string worksheetName = "Errors Log";
        excel.GetOrCreateWorksheet(worksheetName);

        var counter = 0;
        string[] prevMessages = [];
        ICanPackageParsed? prevPackage = null;
        foreach (var package in logParser.GetPackages(filterIds, start, end))
        {
            if (prevPackage != null &&
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
            if (packageMessages.Length == 0)
            {
                continue;
            }
            if (prevMessages.SequenceEqual(packageMessages))
            {
                continue;
            }
            var row = new [] { package.Id.ToString(), package.Name }
                .Concat(packageMessages.ToArray())
                .ToArray();
            excel.AddRow(worksheetName, row);
            counter++;
            prevPackage = package;
            prevMessages = packageMessages;
        }
        Console.WriteLine($"Added {counter} messages to excel.");
    }
}