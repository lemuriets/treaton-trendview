using System.Collections.Immutable;
using System.Text.RegularExpressions;
using LogDecoder.Parser.Contracts;

namespace LogDecoder.Parser;

public class LogFilesAggregator : ILogFilesAggregator
{
    private readonly List<string> _sortedFiles;
    private readonly List<string> _sortedFilenames;

    public IReadOnlyList<string> SortedFiles => _sortedFiles;
    public IReadOnlyList<string> SortedFilenames => _sortedFilenames;

    public LogFilesAggregator(string folder, Func<string, object> sortKeySelector, Regex fileNamePattern) 
    {
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"The specified folder with logs was not found '{folder}'");
        }

        _sortedFiles = Directory
            .GetFileSystemEntries(folder)
            .Where(entry => fileNamePattern.IsMatch(Path.GetFileName(entry)))
            .OrderBy(sortKeySelector)
            .Select(Path.GetFullPath)
            .ToList();

        _sortedFilenames = _sortedFiles.Select(Path.GetFileNameWithoutExtension).ToList();
    }
    
    public IReadOnlyList<string> GetRange(string startFilename, string endFilename)
    {
        var startIndex = _sortedFilenames.IndexOf(startFilename);
        var endIndex = _sortedFilenames.IndexOf(endFilename);

        if (startIndex == -1 || endIndex == -1)
        {
            throw new InvalidOperationException($"Range files not found. Start: {startFilename}, End: {endFilename}");
        }
        if (startIndex > endIndex)
        {
            throw new ArgumentOutOfRangeException($"Start file goes after end file. ({startFilename} > {endFilename})");
        }
        return _sortedFiles.GetRange(startIndex, endIndex - startIndex + 1);
    }
}
