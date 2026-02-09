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
