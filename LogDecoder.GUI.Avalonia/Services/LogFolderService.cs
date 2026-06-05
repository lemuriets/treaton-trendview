using LogDecoder.Helpers;
using LogDecoder.Parser;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>Inspects a folder for parseable log files.</summary>
public sealed class LogFolderService
{
    public bool HasLogFiles(string folder)
    {
        var filesAggregator = new LogFilesAggregator(folder, Path.GetFileName, LogParser.FilenameTemplateRegex());
        return filesAggregator.SortedFiles.Count > 0;
    }
}
