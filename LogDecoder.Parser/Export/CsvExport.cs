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
    private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("ru-RU");

    public void ToCsv(
        string logsFolder,
        string outputFolder,
        IReadOnlySet<int> filterIds,
        DateTime start,
        DateTime end,
        IReadOnlySet<PackageTechStatus> techStatusesToParse,
        bool ignoreDuplicates = false,
        bool excludeEmptyTimestamps = false,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputFolder);

        var csvFilePath = Path.Combine(outputFolder, $"ИВЛ {DateTime.Now:dd.MM.yyyy HH-mm-ss}.csv");
        logger.LogDebug("Exporting data from: {LogsFolder}. To: {CsvFilePath}. Ids: [{Ids}]",
            logsFolder,
            csvFilePath,
            string.Join(',', filterIds));

        using var csvSession = new CsvSession(csvFilePath);
        var csvWriter = new CsvWriter(csvSession);

        csvWriter.AddRow(["Id", "Имя", "Время последнего IdSynchro", "[ИмяПараметра; Значение]...", "[Сообщение]..."]);

        var rowCounter = WriteDataRows(csvWriter, filterIds, start, end, ignoreDuplicates, excludeEmptyTimestamps, cancellationToken);

        logger.LogDebug("Added {RowCounter} rows to csv.", rowCounter);
    }

    private int WriteDataRows(
        CsvWriter csvWriter,
        IReadOnlySet<int> filterIds,
        DateTime start,
        DateTime end,
        bool ignoreDuplicates,
        bool excludeEmptyTimestamps,
        CancellationToken cancellationToken)
    {
        var rowCounter = 0;
        List<string> prevMessages = [];
        ICanPackageParsed? prevPackage = null;
        string lastDateTimeStr = "";

        foreach (var package in logParser.GetPackages(filterIds, start, end))
        {
            cancellationToken.ThrowIfCancellationRequested();

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

            if (ShouldSkip(package, prevPackage, prevMessages, packageMessages, packageNumericData,
                    excludeEmptyTimestamps, ignoreDuplicates))
            {
                continue;
            }

            csvWriter.AddRow(BuildRow(package, lastDateTimeStr, packageMessages, packageNumericData));

            rowCounter++;
            prevPackage = package;
            prevMessages = packageMessages;
        }

        return rowCounter;
    }

    private static bool ShouldSkip(
        ICanPackageParsed package,
        ICanPackageParsed? prevPackage,
        List<string> prevMessages,
        List<string> currentMessages,
        IReadOnlyCollection<NumericDataItem> numericData,
        bool excludeEmptyTimestamps,
        bool ignoreDuplicates)
    {
        if (excludeEmptyTimestamps &&
            prevPackage != null &&
            package.Id == IdSynchro.Id &&
            prevPackage.Id == package.Id)
        {
            return true;
        }
        if (ignoreDuplicates && prevMessages.SequenceEqual(currentMessages) && numericData.Count == 0)
        {
            return true;
        }
        return false;
    }

    private static bool ShouldExport(ICanPackageParsed package, IReadOnlySet<PackageTechStatus> techStatusesToParse)
    {
        return techStatusesToParse.Count == 0 || techStatusesToParse.Contains(package.TechStatus);
    }

    private static List<string> BuildRow(ICanPackageParsed package, string datetimeStr, IEnumerable<string> messages, IEnumerable<NumericDataItem> data)
    {
        var row = new List<string>
        {
            package.Id.ToString(),
            package.Name,
            datetimeStr,
        };
        foreach (var item in data)
        {
            row.Add(item.Name);
            row.Add(Math.Round(item.Value, RoundTo).ToString(NumberCulture));
        }
        row.AddRange(messages);
        return row;
    }
}
