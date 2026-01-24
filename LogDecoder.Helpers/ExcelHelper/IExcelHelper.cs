namespace LogDecoder.Helpers;

public interface IExcelHelper
{
    void MakeHeader(string[] rowData);
    void AddRow(string[] rowData);
    void MakePlot(object[] data);
}