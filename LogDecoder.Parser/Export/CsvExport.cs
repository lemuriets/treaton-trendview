using System.Text;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.Helpers.Csv;

namespace LogDecoder.Parser.Export;

public class CsvExport(LogParser logParser)
{
    public void ToCsv(string logsFolder, string outputFolder, IReadOnlySet<int> filterIds, DateTime start, DateTime end, PackageTechStatus[] techStatusesToParse, bool ignoreDuplicates = false, bool excludeEmptyTimestamps = false)
    {
        Directory.CreateDirectory(outputFolder);

        var csvFilePath = Path.Combine(outputFolder, $"ИВЛ {DateTime.Now:dd.MM.yyyy HH-mm-ss}.csv");
        Console.WriteLine($"Exporting data from: {logsFolder}. To: {csvFilePath}. Ids: [{string.Join(',', filterIds)}]");

        using var csvSession = new CsvSession(csvFilePath);
        var csvWriter = new CsvWriter(csvSession);

        csvWriter.AddRow(["Id", "Имя", "Сообщения", "Данные"]);

        var rowCounter = 0;
        string[] prevMessages = [];
        ICanPackageParsed? prevPackage = null;
        string lastDateTimeStr = "";

        foreach (var package in logParser.GetPackages(filterIds, start, end))
        {
            if (excludeEmptyTimestamps &&
                prevPackage != null &&
                package.Id == IdSynchro.Id &&
                prevPackage.Id == package.Id)
            {
                continue;
            }
            
            var packageData = package.ParseData();
            if (packageData is null)
            {
                continue;
            }
            // if (!ShouldExport(package, techStatusesToParse))
            // {
            //     continue;
            // }
            
            var packageMessages = packageData.Value.Messages;
            var packageNumericData = packageData.Value.NumericData;

            if (packageMessages.Length == 0 && packageNumericData.Length == 0)
            {
                continue;
            }
            
            if (package.Id == IdSynchro.Id)
            {
                lastDateTimeStr = packageMessages[0];
            }

            if (ignoreDuplicates && prevMessages.SequenceEqual(packageMessages) && packageNumericData.Length == 0)
            {
                continue;
            }

            csvWriter.AddRow(BuildRow(package, lastDateTimeStr, packageMessages, packageNumericData));

            rowCounter++;
            prevPackage = package;
            prevMessages = packageMessages;
        }

        Console.WriteLine($"Added {rowCounter} rows to csv.");
    }

    private static bool ShouldExport(ICanPackageParsed package, PackageTechStatus[] techStatusesToParse)
    {
        if (techStatusesToParse.Length == 0)
        {
            return true;
        }
        return techStatusesToParse.Contains(package.TechStatus);
    }

    private static List<string> BuildRow(ICanPackageParsed package, string datetimeStr, IEnumerable<string> messages, IEnumerable<NumericDataItem> data)
    {
        var row = new List<string>
        {
            package.Id.ToString(),
            package.Name,
        };
        var messagesStr = string.Join('\n', messages);
        var packageTime = package.Id == IdSynchro.Id
            ? messagesStr
            : $"{datetimeStr}\n{messagesStr}";
        row.Add(packageTime);
        foreach (var item in data)
        {
            row.Add(item.Name);
            row.Add(Math.Round(item.Value, 3).ToString());
        }
        return row;
    }
}