using System.Globalization;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using LogDecoder.Helpers.Csv;
using Microsoft.Extensions.Logging;

namespace LogDecoder.Parser.Export;

public class CsvExport(ILogger logger, LogParser logParser)
{
    private const int RoundTo = 3;
    public void ToCsv(
        string logsFolder,
        string outputFolder,
        IReadOnlySet<int> filterIds,
        DateTime start,
        DateTime end,
        PackageTechStatus[] techStatusesToParse,
        bool ignoreDuplicates = false,
        bool excludeEmptyTimestamps = false)
    {
        Directory.CreateDirectory(outputFolder);

        var csvFilePath = Path.Combine(outputFolder, $"ИВЛ {DateTime.Now:dd.MM.yyyy HH-mm-ss}.csv");
        logger.LogInformation("Exporting data from: {LogsFolder}. To: {CsvFilePath}. Ids: [{Ids}]",
            logsFolder,
            csvFilePath,
            string.Join(',', filterIds));

        using var csvSession = new CsvSession(csvFilePath);
        var csvWriter = new CsvWriter(csvSession);

        csvWriter.AddRow(["Id", "Имя", "Время", "Сообщения", "Данные"]);

        var rowCounter = 0;
        List<string> prevMessages = [];
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

            if (packageMessages.Count == 0 && packageNumericData.Count == 0)
            {
                continue;
            }
            
            if (package.Id == IdSynchro.Id)
            {
                lastDateTimeStr = packageMessages[0];
            }

            if (ignoreDuplicates && prevMessages.SequenceEqual(packageMessages) && packageNumericData.Count == 0)
            {
                continue;
            }

            csvWriter.AddRow(BuildRow(package, lastDateTimeStr, packageMessages, packageNumericData));

            rowCounter++;
            prevPackage = package;
            prevMessages = packageMessages;
        }

        logger.LogInformation("Added {RowCounter} rows to csv.",
            rowCounter);
    }

    private static bool ShouldExport(ICanPackageParsed package, PackageTechStatus[] techStatusesToParse)
    {
        return techStatusesToParse.Length == 0 || techStatusesToParse.Contains(package.TechStatus);
    }

    private static List<string> BuildRow(ICanPackageParsed package, string datetimeStr, IEnumerable<string> messages, IEnumerable<NumericDataItem> data)
    {
        var row = new List<string>
        {
            package.Id.ToString(),
            package.Name,
            datetimeStr,
            string.Join('\n', messages),
        };
        foreach (var item in data)
        {
            row.Add(item.Name);
            row.Add(Math.Round(item.Value, RoundTo).ToString(CultureInfo.InvariantCulture));
        }
        return row;
    }
}