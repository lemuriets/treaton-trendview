using System.Drawing;

using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace LogDecoder.Helpers;

public class ExcelSession : IDisposable
{
    public ExcelPackage Package { get; }
    private readonly FileInfo _file;

    public ExcelSession(string filePath, bool rewrite = true)
    {
        ExcelPackage.License.SetNonCommercialPersonal("LogDecoder");

        _file = new FileInfo(filePath);

        if (rewrite && _file.Exists)
        {
            _file.Delete();
        }

        Package = _file.Exists && !rewrite
            ? new ExcelPackage(_file)
            : new ExcelPackage();
    }

    public void Dispose()
    {
        if (Package.Workbook.Worksheets.Count > 0)
        {
            Package.SaveAs(_file);
        }
        Package.Dispose();
    }
}

public class ExcelHelper : IDisposable
{
    private readonly ExcelPackage _package;
    private Dictionary<string, int> _sheetIndexes = new();
    private readonly Dictionary<string, List<string[]>> _sheetBuckets = new();
    public ExcelHelper(ExcelPackage package)
    {
        _package = package;
    }

    public ExcelWorksheet GetOrCreateWorksheet(string sheetName)
    {
        var worksheet = GetWorksheetByName(sheetName);
        if (worksheet == null)
        {
            _sheetIndexes[sheetName] = 0;
            _sheetBuckets[sheetName] = new();
            worksheet = _package.Workbook.Worksheets.Add(sheetName);
        }
        return worksheet;
    }

    public ExcelWorksheet GetLastWorksheet(string baseName)
    {
        var worksheetIndex = _sheetIndexes[baseName];
        if (worksheetIndex == 0)
            return GetWorksheetByName(baseName);
        return GetWorksheetByName($"{baseName}_{worksheetIndex}");
    }

    public void AddRow(string sheetName, params string[] values)
    {
        AddRow(sheetName, Color.Empty, values);
    }

    public void AddRow(string sheetName, Color color, params string[] values)
    {
        var worksheetBucket = _sheetBuckets[sheetName];

        if (worksheetBucket.Count == ExcelPackage.MaxRows)
        {
            Flush(sheetName);

            _sheetIndexes[sheetName] += 1;
            var nextListName = $"{sheetName}_{_sheetIndexes[sheetName]}";
            worksheetBucket = _sheetBuckets[nextListName] = new();

            GetOrCreateWorksheet(nextListName);
        }

        worksheetBucket.Add(values);
    }

    //public void AddChartByColumn(string sheetName, int columnIndex, bool hasHeader = true)
    //{
    //    var worksheet = GetWorksheetByName(sheetName);
    //    if (worksheet.Dimension == null)
    //        throw new InvalidOperationException("Worksheet is empty.");

    //    var startRow = hasHeader ? 2 : 1;
    //    var endRow = worksheet.Dimension.End.Row;

    //    var chartColumn = worksheet.Dimension.End.Column + 2;
    //    var chartRow = 1;

    //    var chart = worksheet.Drawings.AddChart($"Chart_Column{columnIndex}", eChartType.Line) as ExcelLineChart;
    //    chart.Title.Text = worksheet.Cells[1, columnIndex].Text ?? $"Column {columnIndex}";

    //    chart.Series.Add(
    //        worksheet.Cells[startRow, columnIndex, endRow, columnIndex]
    //    );

    //    chart.SetPosition(chartRow - 1, 0, chartColumn - 1, 0);
    //    chart.SetSize(600, 400);
    //}

    private ExcelWorksheet GetWorksheetByName(string name)
    {
        return _package.Workbook.Worksheets[name];
    }

    private void Flush(string sheetName)
    {
        if (_sheetBuckets[sheetName].Count == 0)
            return;

        var ws = GetLastWorksheet(sheetName);
        ws.Cells[1, 1].LoadFromArrays(_sheetBuckets[sheetName]);

        _sheetBuckets[sheetName].Clear();
    }

    public void Dispose()
    {
        foreach (var sheetName in _sheetBuckets.Keys.ToList())
        {
            Flush(sheetName);
        }
    }
}
