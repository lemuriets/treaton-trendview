using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LogDecoder.Infrastructure.Logging;
using Serilog.Events;

namespace LogDecoder.GUI.Avalonia.ViewModels;

public partial class DebugLogViewModel : ObservableObject, IDisposable
{
    private const string TimestampFormat = "HH:mm:ss.fff";

    private readonly InMemoryLogSink _sink = InMemoryLogSink.Instance;

    private LogEventLevel? _selectedLevel;

    public DebugLogViewModel()
    {
        foreach (var entry in _sink.GetAll())
        {
            AddIfMatchesFilter(entry);
        }

        _sink.LogReceived += OnLogReceived;
    }

    public ObservableCollection<LogLineItem> Lines { get; } = [];

    public string[] LevelFilters { get; } = ["All", "Verbose", "Debug", "Info", "Warning", "Error", "Fatal"];

    [ObservableProperty]
    private string _selectedFilter = "All";

    partial void OnSelectedFilterChanged(string value)
    {
        _selectedLevel = value switch
        {
            "Verbose" => LogEventLevel.Verbose,
            "Debug" => LogEventLevel.Debug,
            "Info" => LogEventLevel.Information,
            "Warning" => LogEventLevel.Warning,
            "Error" => LogEventLevel.Error,
            "Fatal" => LogEventLevel.Fatal,
            _ => null
        };

        Rebuild();
    }

    private void OnLogReceived(LogEvent entry)
    {
        Dispatcher.UIThread.Post(() => AddIfMatchesFilter(entry));
    }

    private void AddIfMatchesFilter(LogEvent entry)
    {
        if (_selectedLevel is { } level && entry.Level != level)
        {
            return;
        }

        Lines.Add(ToLineItem(entry));
    }

    private void Rebuild()
    {
        Lines.Clear();

        foreach (var entry in _sink.GetAll())
        {
            AddIfMatchesFilter(entry);
        }
    }

    private static LogLineItem ToLineItem(LogEvent entry)
    {
        var ts = entry.Timestamp.ToString(TimestampFormat);
        var level = FormatLevel(entry.Level);
        var message = entry.RenderMessage();

        var text = $"[{level}] ({ts}) {message}";

        if (entry.Exception is { } ex)
        {
            text += Environment.NewLine + ex;
        }

        return new LogLineItem(text, entry.Level);
    }

    private static string FormatLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => level.ToString()[..3].ToUpperInvariant()
        };
    }

    public void Dispose()
    {
        _sink.LogReceived -= OnLogReceived;
    }
}

public sealed record LogLineItem(string Text, LogEventLevel Level);
