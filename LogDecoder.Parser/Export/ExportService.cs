using System.Reflection.Metadata.Ecma335;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data.Contracts;
using LogDecoder.Helpers;

namespace LogDecoder.Parser.Export;

public class ExportService(ILogFileScanner logFileScanner) : IExportService
{
    public void ToExcel(string file, string outputFolder, HashSet<int> filterIds, DateTime start, DateTime end)
    {
        Console.WriteLine($"Processing file: {file}");

        var baseFilename = Path.GetFileName(file);
        var excelFilePath = Path.Combine(outputFolder, baseFilename + ".xlsx");
        using var excelSession = new ExcelSession(excelFilePath);
        using var excel = new ExcelHelper(excelSession.Package);

        const string worksheetName = "Все пакеты";
        excel.GetOrCreateWorksheet(worksheetName);

        CreateExcelHeaders(excel, worksheetName, filterIds);

        foreach (var package in logFileScanner.ExtractAllPackages(filterIds, start, end))
        {
            var packageData = package.ParseData();
            if (packageData is null)
            {
                continue;
            }
            var packageErrors = packageData.Value.Messages;
            var row = new [] { package.Id.ToString(), package.Name }
                .Concat(packageErrors.ToArray())
                .ToArray();
            excel.AddRow(worksheetName, row);
        }
    }

    private void CreateExcelHeaders(ExcelHelper excel, string sheetName, HashSet<int> ids)
    {
        foreach (var id in ids)
        {
            string[] row = [id.ToString()];
            excel.AddRow(sheetName, row);
        }
    }
}