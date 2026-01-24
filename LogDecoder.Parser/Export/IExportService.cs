namespace LogDecoder.Parser.Export;

public interface IExportService
{
    void ToExcel(string filePath, string outputFolder, HashSet<int> filterIds, DateTime start, DateTime end);
}