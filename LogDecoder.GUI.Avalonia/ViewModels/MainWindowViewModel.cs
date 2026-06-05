using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogDecoder.GUI.Avalonia.Localization;
using LogDecoder.GUI.Avalonia.Models;
using LogDecoder.GUI.Avalonia.Services;
using LogDecoder.Parser;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";
    private const string AppName = "TrendView_IVL";

    private readonly ILogger _logger;
    private readonly PackageCatalog _packageCatalog;
    private readonly IndexingService _indexing;
    private readonly ExportService _export;
    private readonly LogFolderService _logFolders;
    private readonly LanguageService _language;
    private readonly FolderPickerService _folderPicker;
    private readonly FolderLauncherService _folderLauncher;
    private readonly DialogService _dialogs;
    private readonly Action<LogParser> _openTrends;
    private readonly string _appVersion;

    private readonly FolderSelection _inputFolderSelection = new();
    private readonly FolderSelection _outputFolderSelection = new();

    private StatusText? _exportStatus;
    private StatusText? _indexStatus;
    private StatusText? _inputFolderMessage = new("InputFolderNotSelected", []);
    private StatusText? _outputFolderMessage = new("OutputFolderNotSelected", []);

    private IReadOnlySet<int> _selectedPackageIds = new HashSet<int>();

    private CancellationTokenSource? _exportCts;
    private CancellationTokenSource? _indexCts;
    private Task? _exportTask;

    public MainWindowViewModel(
        ILogger logger,
        PackageCatalog packageCatalog,
        IndexingService indexing,
        ExportService export,
        LogFolderService logFolders,
        LanguageService language,
        FolderPickerService folderPicker,
        FolderLauncherService folderLauncher,
        DialogService dialogs,
        Action<LogParser> openTrends,
        string appVersion)
    {
        _logger = logger;
        _packageCatalog = packageCatalog;
        _indexing = indexing;
        _export = export;
        _logFolders = logFolders;
        _language = language;
        _folderPicker = folderPicker;
        _folderLauncher = folderLauncher;
        _dialogs = dialogs;
        _openTrends = openTrends;
        _appVersion = appVersion;

        _title = $"{AppName} v{appVersion}";
        _currentCulture = _language.CurrentCulture;
        _startDateTimeText = DateTime.MinValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        _endDateTimeText = DateTime.Now.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        Packages = new ObservableCollection<PackageItem>(_packageCatalog.GetExportablePackages());

        _indexing.IndexStarted += OnIndexStarted;
        _indexing.IndexFinished += OnIndexFinished;
        LocalizationManager.Instance.PropertyChanged += OnLanguageChanged;

        ApplyInputFolderLabel();
        ApplyOutputFolderLabel();
        Validate();
    }

    public ObservableCollection<PackageItem> Packages { get; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _currentCulture;

    [ObservableProperty]
    private string _inputFolderText = string.Empty;
    [ObservableProperty]
    private IBrush _inputFolderBrush = Brushes.Gray;
    [ObservableProperty]
    private bool _inputFolderIsLink;

    [ObservableProperty]
    private string _outputFolderText = string.Empty;
    [ObservableProperty]
    private IBrush _outputFolderBrush = Brushes.Gray;
    [ObservableProperty]
    private bool _outputFolderIsLink;

    [ObservableProperty]
    private string _startDateTimeText;
    [ObservableProperty]
    private string _endDateTimeText;
    [ObservableProperty]
    private string _dateErrorText = string.Empty;

    [ObservableProperty]
    private bool _ignoreDuplicates = true;
    [ObservableProperty]
    private bool _excludeEmptyTimestamps = true;

    [ObservableProperty]
    private string _exportStatusText = string.Empty;
    [ObservableProperty]
    private IBrush _exportStatusBrush = Brushes.Green;
    [ObservableProperty]
    private string _indexStatusText = string.Empty;

    [ObservableProperty]
    private bool _isIndexing;
    [ObservableProperty]
    private bool _isExporting;

    public bool IsBusy => IsIndexing || IsExporting;

    partial void OnStartDateTimeTextChanged(string value) => OnInputChanged();
    partial void OnEndDateTimeTextChanged(string value) => OnInputChanged();

    // Matches the old InputsChanged handler: clear the (now stale) export status and re-validate.
    private void OnInputChanged()
    {
        SetExportStatus(null, Brushes.Green);
        Validate();
    }

    partial void OnIsIndexingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        RefreshCommands();
    }

    partial void OnIsExportingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        RefreshCommands();
    }

    // Called by the view when the multi-select package list changes (the selection
    // mechanics stay in code-behind; the view-model only needs the resulting IDs).
    public void SetSelectedPackageIds(IReadOnlySet<int> ids)
    {
        _selectedPackageIds = ids;
        SetExportStatus(null, Brushes.Green);
        RefreshCommands();
    }

    [RelayCommand(CanExecute = nameof(CanOpenLogsFolder))]
    private async Task OpenLogsFolderAsync()
    {
        SetIndexing(true, "SelectingLogsFolder");

        try
        {
            var selectedFolder = await _folderPicker.PickFolderAsync(Localizer.L("SelectFolderDialogTitle"));
            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            if (!await Task.Run(() => _logFolders.HasLogFiles(selectedFolder)))
            {
                ResetInputFolder("NoLogFilesInFolder", Brushes.Red);
                return;
            }

            SetExportStatus(null, Brushes.Green);

            SetInputFolder(selectedFolder);
            SetDefaultOutputFolderIfEmpty(selectedFolder);
            _indexing.CreateSession(selectedFolder);

            _indexCts?.Dispose();
            var cts = new CancellationTokenSource();
            _indexCts = cts;

            try
            {
                await _indexing.IndexAsync(cts.Token);
                FillStartAndEndDateTimeFromLogsIfNeeded();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Indexing cancelled");
                SetIndexStatus("IndexingCancelled");
                ResetInputFolder("InputFolderNotSelected", Brushes.Gray);
            }
            finally
            {
                _indexCts = null;
                cts.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while selecting input folder");
            ResetInputFolder("InputFolderSelectionError", Brushes.Red, ex.Message);
        }
        finally
        {
            SetIndexing(false, string.Empty);
        }
    }

    private bool CanOpenLogsFolder() => !IsBusy;

    [RelayCommand]
    private async Task OpenExportFolderAsync()
    {
        try
        {
            var folder = await _folderPicker.PickFolderAsync(Localizer.L("SelectFolderDialogTitle"));
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            SetOutputFolder(folder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while selecting output folder");
            ResetOutputFolder("OutputFolderSelectionError", Brushes.Red, ex.Message);
        }
        finally
        {
            Validate();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        if (!TryGetValidatedRange(out var start, out var end, out _) || _indexing.Current is not { } session)
        {
            Validate();
            return;
        }

        var ids = new HashSet<int>(_packageCatalog.RequiredExportIds);
        ids.UnionWith(_selectedPackageIds);

        var request = new ExportRequest(
            _inputFolderSelection.Path,
            _outputFolderSelection.Path,
            ids,
            start,
            end,
            IgnoreDuplicates,
            ExcludeEmptyTimestamps);

        _exportCts?.Dispose();
        var cts = new CancellationTokenSource();
        _exportCts = cts;

        SetExporting(true, "ExportingWait");

        try
        {
            _exportTask = _export.ExportAsync(session, request, cts.Token);
            await _exportTask;

            SetExportStatus("ExportSuccess", Brushes.Green);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("CSV export cancelled");
            SetExportStatus("ExportCancelled", Brushes.Orange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing IVL logs");
            SetExportStatus("ExportError", Brushes.Red, ex.Message);
        }
        finally
        {
            _exportCts = null;
            _exportTask = null;
            cts.Dispose();
            SetExporting(false);
        }
    }

    private bool CanExport()
    {
        if (IsBusy || _indexing.Current is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_inputFolderSelection.Path) || string.IsNullOrEmpty(_outputFolderSelection.Path))
        {
            return false;
        }

        return _selectedPackageIds.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanCancelExport))]
    private void CancelExport()
    {
        var cts = _exportCts;
        if (cts is null)
        {
            return;
        }

        cts.Cancel();
        SetExportStatus("ExportCancelling", Brushes.Orange);
    }

    private bool CanCancelExport() => IsExporting;

    [RelayCommand(CanExecute = nameof(CanOpenTrends))]
    private void OpenTrends()
    {
        if (_indexing.Current is not { } session || !CanOpenTrends())
        {
            return;
        }

        _openTrends(session.Parser);
    }

    private bool CanOpenTrends() => !IsBusy && _indexing.HasIndex;

    [RelayCommand]
    private void OpenInputFolder()
    {
        if (_inputFolderSelection.IsSelected)
        {
            _folderLauncher.OpenFolder(_inputFolderSelection.Path);
        }
    }

    [RelayCommand]
    private void OpenOutputFolder()
    {
        if (_outputFolderSelection.IsSelected)
        {
            _folderLauncher.OpenFolder(_outputFolderSelection.Path);
        }
    }

    [RelayCommand]
    private void ChangeLanguage(string culture)
    {
        _logger.LogDebug("App language changed to {Lang}", culture);
        _language.SetLanguage(culture);
    }

    [RelayCommand]
    private async Task AboutAsync()
    {
        await _dialogs.ShowMessageAsync(
            Localizer.L("AboutTitle"),
            Localizer.F("AboutText", _appVersion));
    }

    private void RefreshCommands()
    {
        OpenLogsFolderCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        CancelExportCommand.NotifyCanExecuteChanged();
        OpenTrendsCommand.NotifyCanExecuteChanged();
    }

    private void OnIndexStarted()
    {
        Dispatcher.UIThread.Post(() =>
        {
            SetIndexStatus("IndexingWait");
            SetIndexing(true);
        });
    }

    private void OnIndexFinished()
    {
        Dispatcher.UIThread.Post(() =>
        {
            SetIndexStatus(null);
            SetIndexing(false);
        });
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyInputFolderLabel();
        ApplyOutputFolderLabel();
        ExportStatusText = Render(_exportStatus);
        IndexStatusText = Render(_indexStatus);
        Validate();
        CurrentCulture = _language.CurrentCulture;
    }

    private void SetIndexing(bool value, string? statusKey = null)
    {
        IsIndexing = value;
        ApplyProvidedExportStatus(statusKey);
    }

    private void SetExporting(bool value, string? statusKey = null)
    {
        IsExporting = value;
        ApplyProvidedExportStatus(statusKey);
    }

    private void ApplyProvidedExportStatus(string? statusKey)
    {
        if (statusKey is null)
        {
            return;
        }
        // An empty key is used by callers as "clear the status".
        SetExportStatus(statusKey.Length == 0 ? null : statusKey, Brushes.Green);
    }

    private void SetExportStatus(string? key, IBrush brush, params object[] args)
    {
        _exportStatus = key is null ? null : new StatusText(key, args);
        ExportStatusBrush = brush;
        ExportStatusText = Render(_exportStatus);
    }

    private void SetIndexStatus(string? key, params object[] args)
    {
        _indexStatus = key is null ? null : new StatusText(key, args);
        IndexStatusText = Render(_indexStatus);
    }

    private void SetInputFolder(string folder)
    {
        _inputFolderMessage = null;
        _inputFolderSelection.Path = folder;
        InputFolderText = folder;
        InputFolderBrush = Brushes.Green;
        InputFolderIsLink = true;
        RefreshCommands();
    }

    private void ResetInputFolder(string messageKey, IBrush foreground, params object[] args)
    {
        _inputFolderMessage = new StatusText(messageKey, args);
        _inputFolderSelection.Path = string.Empty;
        InputFolderText = Render(_inputFolderMessage);
        InputFolderBrush = foreground;
        InputFolderIsLink = false;

        _indexing.Reset();
        RefreshCommands();
    }

    private void SetOutputFolder(string folder)
    {
        _outputFolderMessage = null;
        _outputFolderSelection.Path = folder;
        OutputFolderText = folder;
        OutputFolderBrush = Brushes.Green;
        OutputFolderIsLink = true;
        RefreshCommands();
    }

    private void ResetOutputFolder(string messageKey, IBrush foreground, params object[] args)
    {
        _outputFolderMessage = new StatusText(messageKey, args);
        _outputFolderSelection.Path = string.Empty;
        OutputFolderText = Render(_outputFolderMessage);
        OutputFolderBrush = foreground;
        OutputFolderIsLink = false;
        RefreshCommands();
    }

    private void SetDefaultOutputFolderIfEmpty(string folder)
    {
        if (!string.IsNullOrEmpty(_outputFolderSelection.Path))
        {
            return;
        }
        SetOutputFolder(folder);
    }

    private void ApplyInputFolderLabel()
    {
        if (_inputFolderMessage is null)
        {
            return;
        }
        InputFolderText = Render(_inputFolderMessage);
    }

    private void ApplyOutputFolderLabel()
    {
        if (_outputFolderMessage is null)
        {
            return;
        }
        OutputFolderText = Render(_outputFolderMessage);
    }

    private void FillStartAndEndDateTimeFromLogsIfNeeded()
    {
        if (!TryGetDateTime(StartDateTimeText, out var currentStart) || currentStart != DateTime.MinValue)
        {
            return;
        }

        if (_indexing.MinDateTime is { } start)
        {
            StartDateTimeText = start.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }
        if (_indexing.MaxDateTime is { } end)
        {
            EndDateTimeText = end.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }
    }

    private void Validate()
    {
        TryGetValidatedRange(out _, out _, out var error);
        DateErrorText = error ?? string.Empty;
        RefreshCommands();
    }

    private bool TryGetValidatedRange(out DateTime start, out DateTime end, out string? error)
    {
        var startOk = TryGetDateTime(StartDateTimeText, out start);
        var endOk = TryGetDateTime(EndDateTimeText, out end);
        if (!startOk || !endOk)
        {
            error = Localizer.F("InvalidDateFormat", DateTimeFormat);
            return false;
        }

        if (end < start)
        {
            error = Localizer.L("EndBeforeStart");
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryGetDateTime(string? value, out DateTime dateTime)
    {
        return DateTime.TryParseExact(
            value,
            DateTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dateTime);
    }

    private static string Render(StatusText? status)
    {
        if (status is null)
        {
            return string.Empty;
        }

        var (key, args) = status.Value;
        return args.Length == 0 ? Localizer.L(key) : Localizer.F(key, args);
    }

    public void Dispose()
    {
        _indexing.IndexStarted -= OnIndexStarted;
        _indexing.IndexFinished -= OnIndexFinished;
        LocalizationManager.Instance.PropertyChanged -= OnLanguageChanged;

        _indexCts?.Cancel();
        _exportCts?.Cancel();

        var pendingExport = _exportTask;
        if (pendingExport is not null)
        {
            try
            {
                pendingExport.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
            }
        }

        _indexing.Reset();
    }
}

internal sealed class FolderSelection
{
    public string Path { get; set; } = string.Empty;
    public bool IsSelected => !string.IsNullOrEmpty(Path);
}

internal readonly record struct StatusText(string Key, object[] Args);
