using System.Text;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Helpers.Csv;

namespace LogDecoder.Parser.Export;

public class CsvExport(LogParser logParser)
{
    public void ToCsv(string logsFolder, string outputFolder, IReadOnlySet<int> filterIds, DateTime start, DateTime end, PackageTechStatus[] techStatusesToParse, bool ignoreDuplicates = false, bool excludeEmptyTimestamps = false)
    {
        Directory.CreateDirectory(outputFolder);

        var csvFilePath = Path.Combine(outputFolder, $"Errors Log {DateTime.Now:dd.MM.yyyy HH-mm-ss}.csv");
        Console.WriteLine($"Exporting data from: {logsFolder}. To: {csvFilePath}. Ids: [{string.Join(',', filterIds)}]");

        using var csvSession = new CsvSession(csvFilePath);
        var csvWriter = new CsvWriter(csvSession);

        csvWriter.AddRow(["Id", "Имя", "Сообщения", "Данные"]);

        var rowCounter = 0;
        string[] prevMessages = [];
        ICanPackageParsed? prevPackage = null;

        foreach (var package in logParser.GetPackages(filterIds, start, end))
        {
            var packageData = package.ParseData();
            if (packageData is null)
            {
                continue;
            }

            // if (!ShouldExport(package, techStatusesToParse))
            // {
            //     continue;
            // }

            if (excludeEmptyTimestamps &&
                prevPackage != null &&
                prevPackage.Id == package.Id &&
                package.Id == IdSynchro.Id)
            {
                continue;
            }

            var packageMessages = packageData.Value.Messages;
            var packageNumericDataStrings = packageData.Value.GetNumericDataStrings();

            if (packageMessages.Length == 0 && packageNumericDataStrings.Length == 0)
            {
                continue;
            }

            if (ignoreDuplicates && prevMessages.SequenceEqual(packageMessages) && packageNumericDataStrings.Length == 0)
            {
                continue;
            }

            csvWriter.AddRow(BuildRow(package, packageMessages, packageNumericDataStrings));

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

    private static List<string> BuildRow(ICanPackageParsed package, IEnumerable<string> messages, IEnumerable<string> data)
    {
        var row = new List<string>
        {
            package.Id.ToString(),
            package.Name,
            string.Join('\n', messages),
        };
        row.AddRange(data);

        return row;
    }
}