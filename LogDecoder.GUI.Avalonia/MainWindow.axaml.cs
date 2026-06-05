using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using LogDecoder.GUI.Avalonia.Localization;
using LogDecoder.GUI.Avalonia.Models;
using LogDecoder.GUI.Avalonia.Services;
using LogDecoder.Helpers;
using LogDecoder.Infrastructure.Logging;
using LogDecoder.Parser;
using LogDecoder.Parser.Export;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace LogDecoder.GUI.Avalonia;

public partial class MainWindow : Window
{
    private const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";
    private const string AppName = "TrendView_IVL";

    private static readonly HashSet<PackageTechStatus> ExportTechStatuses =
    [
        PackageTechStatus.Warning,
        PackageTechStatus.Error,
        PackageTechStatus.Critical,
        PackageTechStatus.Info,
        PackageTechStatus.Ok
    ];

    private readonly ICanPackageFactory _factory;
    private readonly ILogger _logger;
    private readonly LoggerProvider _loggerProvider;
    private readonly string _appVersion = AppVersionProvider.GetVersion();
    private readonly IReadOnlySet<int> _requiredExportIds = new HashSet<int> { IdSynchro.Id };

    private bool _isIndexing;
    private bool _isExporting;
    private bool _isUpdatingSelectAll;

    private bool IsBusy => _isIndexing || _isExporting;

    private readonly FolderSelection _inputFolderSelection = new();
    private readonly FolderSelection _outputFolderSelection = new();
    private ParserSession? _session;
    private List<PackageItem> _packageItems = [];
    private CancellationTokenSource? _exportCancellationTokenSource;
    private Task? _exportTask;
    private CancellationTokenSource? _indexCancellationTokenSource;

    private readonly LanguageSettingsService _languageSettings;

    // Persistent, re-localizable UI state: store the resource key + args (not the
    // formatted string) so these labels can be re-rendered when the language changes.
    private StatusText? _exportStatus;
    private IBrush _exportStatusBrush = Brushes.Green;
    private StatusText? _indexStatus;
    private StatusText? _inputFolderMessage = new("InputFolderNotSelected", []);
    private IBrush _inputFolderBrush = Brushes.Gray;
    private StatusText? _outputFolderMessage = new("OutputFolderNotSelected", []);
    private IBrush _outputFolderBrush = Brushes.Gray;

    public MainWindow()
    {
        _loggerProvider = new LoggerProvider();
        _logger = _loggerProvider.CreateLogger<MainWindow>();
        _factory = new CanPackageFactory();
        _languageSettings = new LanguageSettingsService(_logger);

        InitializeComponent();

        DataContext = LocalizationManager.Instance;
        LocalizationManager.Instance.PropertyChanged += LocalizationChanged;

        FillWidgets();
        SetWindowTitle();
        ConnectEvents();
        UpdateLanguageCheckmarks();

        _logger.LogDebug("GUI initialization finished");
    }

    protected override void OnClosed(EventArgs e)
    {
        LocalizationManager.Instance.PropertyChanged -= LocalizationChanged;
        UnsubscribeParserEvents(_session);

        _indexCancellationTokenSource?.Cancel();
        _exportCancellationTokenSource?.Cancel();
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

        _loggerProvider.Dispose();

        base.OnClosed(e);
    }

    private void LocalizationChanged(object? sender, EventArgs e)
    {
        SetWindowTitle();
        RefreshLocalizedTexts();
        CheckInputs();
    }

    private void SetWindowTitle()
    {
        Title = $"{AppName} v{_appVersion}";
    }

    private void RefreshLocalizedTexts()
    {
        ApplyInputFolderLabel();
        ApplyOutputFolderLabel();
        ApplyExportStatus();
        ApplyIndexStatus();
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

    private void ApplyExportStatus()
    {
        TxtExportStatus.Text = Render(_exportStatus);
        TxtExportStatus.Foreground = _exportStatusBrush;
    }

    private void ApplyIndexStatus()
    {
        TxtIndexStatus.Text = Render(_indexStatus);
    }

    private void ApplyInputFolderLabel()
    {
        if (_inputFolderMessage is null)
        {
            return;
        }

        _inputFolderSelection.Path = string.Empty;
        TxtSelectedInputFolder.Text = Render(_inputFolderMessage);
        TxtSelectedInputFolder.Foreground = _inputFolderBrush;
        SetFolderLinkState(TxtSelectedInputFolder, false);
    }

    private void ApplyOutputFolderLabel()
    {
        if (_outputFolderMessage is null)
        {
            return;
        }

        _outputFolderSelection.Path = string.Empty;
        TxtSelectedOutputFolder.Text = Render(_outputFolderMessage);
        TxtSelectedOutputFolder.Foreground = _outputFolderBrush;
        SetFolderLinkState(TxtSelectedOutputFolder, false);
    }

    private void SetExportStatus(string? key, IBrush brush, params object[] args)
    {
        _exportStatus = key is null ? null : new StatusText(key, args);
        _exportStatusBrush = brush;
        ApplyExportStatus();
    }

    private void SetIndexStatus(string? key, params object[] args)
    {
        _indexStatus = key is null ? null : new StatusText(key, args);
        ApplyIndexStatus();
    }

    private void FillWidgets()
    {
        StartDateTime.Text = DateTime.MinValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        EndDateTime.Text = DateTime.Now.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        _packageItems = _factory
            .GetIdsWithNames(excludeIds: _requiredExportIds)
            .Select(p => new PackageItem { Id = p.Id, Name = p.Name })
            .OrderBy(p => p.Id)
            .ToList();

        PackageIdList.ItemsSource = _packageItems;

        RefreshLocalizedTexts();

        _logger.LogDebug("FillWidgets() completed");
    }

    private void ConnectEvents()
    {
        BtnExportCsv.Click += BtnExportCsvClick;
        BtnCancelExport.Click += BtnCancelExport_Click;

        StartDateTime.TextChanged += InputsChanged;
        EndDateTime.TextChanged += InputsChanged;
        PackageIdList.SelectionChanged += InputsChanged;

        ChkSelectAllPackages.Checked += ChkSelectAllPackagesChecked;
        ChkSelectAllPackages.Unchecked += ChkSelectAllPackagesUnchecked;

        _logger.LogDebug("ConnectEvents() completed");
    }

    private async Task<string> SelectFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return string.Empty;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = Localizer.L("SelectFolderDialogTitle")
        });

        if (folders.Count == 0)
        {
            return string.Empty;
        }
        return folders[0].TryGetLocalPath() ?? string.Empty;
    }

    private static bool HasLogFiles(string folder)
    {
        var filesAggregator = new LogFilesAggregator(folder, Path.GetFileName, LogParser.FilenameTemplateRegex());
        return filesAggregator.SortedFiles.Count > 0;
    }
    
    private void SetInputFolder(string folder)
    {
        _inputFolderMessage = null;
        SetFolder(_inputFolderSelection, TxtSelectedInputFolder, folder, Brushes.Green, true);
    }

    private void ResetInputFolder(string messageKey, IBrush foreground, params object[] args)
    {
        _inputFolderMessage = new StatusText(messageKey, args);
        _inputFolderBrush = foreground;
        ApplyInputFolderLabel();

        UnsubscribeParserEvents(_session);
        _session = null;
    }

    private void SetOutputFolder(string folder)
    {
        _outputFolderMessage = null;
        SetFolder(_outputFolderSelection, TxtSelectedOutputFolder, folder, Brushes.Green, true);
    }

    private void ResetOutputFolder(string messageKey, IBrush foreground, params object[] args)
    {
        _outputFolderMessage = new StatusText(messageKey, args);
        _outputFolderBrush = foreground;
        ApplyOutputFolderLabel();
    }
    
    private void SetFolder(FolderSelection selection, TextBlock textBlock, string folder, IBrush foreground, bool isLinkState)
    {
        selection.Path = folder;

        textBlock.Text = folder;
        textBlock.Foreground = foreground;

        SetFolderLinkState(textBlock, isLinkState);
    }
    
    private void SetFolderLinkState(TextBlock textBlock, bool isClickable)
    {
        textBlock.Cursor = isClickable
            ? new Cursor(StandardCursorType.Hand)
            : Cursor.Default;

        textBlock.TextDecorations = isClickable
            ? TextDecorations.Underline
            : null;
    }

    private void OpenFolder(FolderSelection folderSelection)
    {
        if (!folderSelection.IsSelected)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(folderSelection.Path) || !Directory.Exists(folderSelection.Path))
        {
            _logger.LogWarning("Cannot open folder. Folder does not exist: {Folder}", folderSelection.Path);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderSelection.Path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed OpenFolder({Folder})", folderSelection.Path);
        }
    }

    private void SelectedInputFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        OpenFolder(_inputFolderSelection);
    }

    private void SelectedOutputFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        OpenFolder(_outputFolderSelection);
    }

    private void SetDefaultOutputFolderIfEmpty(string folder)
    {
        if (!string.IsNullOrEmpty(_outputFolderSelection.Path))
        {
            return;
        }
        SetOutputFolder(folder);
    }

    private ParserSession CreateSession(string selectedFolder)
    {
        UnsubscribeParserEvents(_session);

        var parser = new LogParser(_logger, selectedFolder, _factory);
        var session = new ParserSession(parser, new CsvExport(_logger, parser));

        parser.StartIndex += OnIndexStart;
        parser.FinishIndex += OnIndexFinish;

        _session = session;
        return session;
    }

    private void UnsubscribeParserEvents(ParserSession? session)
    {
        if (session is null)
        {
            return;
        }

        session.Parser.StartIndex -= OnIndexStart;
        session.Parser.FinishIndex -= OnIndexFinish;
    }

    private void FillStartAndEndDateTimeFromLogsIfNeeded(ParserSession session)
    {
        if (!TryGetDateTime(StartDateTime.Text, out var currentStart) || currentStart != DateTime.MinValue)
        {
            return;
        }

        var start = session.Parser.MinDatetime;
        if (start.HasValue)
        {
            StartDateTime.Text = start.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }
        var end = session.Parser.MaxDatetime;
        if (end.HasValue)
        {
            EndDateTime.Text = end.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }
    }

    private void InputsChanged(object? sender, EventArgs e)
    {
        UpdateSelectAllCheckbox();
        CheckInputs();
        SetExportStatus(null, Brushes.Green);
    }

    private void CheckInputs()
    {
        var hasValidRange = TryGetValidatedRange(out _, out _, out var error);
        TxtErrorDateTime.Text = error ?? string.Empty;
        BtnExportCsv.IsEnabled = hasValidRange && CanExport();
    }

    private bool TryGetValidatedRange(out DateTime start, out DateTime end, out string? error)
    {
        var startOk = TryGetDateTime(StartDateTime.Text, out start);
        var endOk = TryGetDateTime(EndDateTime.Text, out end);
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

    private bool CanExport()
    {
        if (IsBusy)
        {
            return false;
        }

        if (_session is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_inputFolderSelection.Path) || string.IsNullOrEmpty(_outputFolderSelection.Path))
        {
            return false;
        }

        if (PackageIdList.SelectedItems is null || PackageIdList.SelectedItems.Count == 0)
        {
            return false;
        }

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

    private void UpdateSelectAllCheckbox()
    {
        if (_isUpdatingSelectAll)
        {
            return;
        }

        _isUpdatingSelectAll = true;
        ChkSelectAllPackages.IsChecked = PackageIdList.SelectedItems?.Count == _packageItems.Count;
        _isUpdatingSelectAll = false;
    }

    private async void BtnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        var session = _session;
        if (!TryGetValidatedRange(out var start, out var end, out _) || !CanExport() || session is null)
        {
            CheckInputs();
            return;
        }

        var selectedIds = GetSelectedIds();
        var inputFolder = _inputFolderSelection.Path;
        var outputFolder = _outputFolderSelection.Path;
        var ignoreDuplicates = ChkIgnoreDuplicates.IsChecked == true;
        var excludeEmptyTimestamps = ChkExcludeEmptyTimestamps.IsChecked == true;

        _exportCancellationTokenSource?.Dispose();
        var cts = new CancellationTokenSource();
        _exportCancellationTokenSource = cts;

        var cancellationToken = cts.Token;

        SetExporting(true, "ExportingWait");

        try
        {
            _exportTask = Task.Run(() =>
            {
                session.Export.ToCsv(
                    inputFolder,
                    outputFolder,
                    selectedIds,
                    start,
                    end,
                    ExportTechStatuses,
                    ignoreDuplicates,
                    excludeEmptyTimestamps,
                    cancellationToken);
            }, cancellationToken);
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
            _exportCancellationTokenSource = null;
            _exportTask = null;
            cts.Dispose();

            SetExporting(false);
        }
    }
    
    private void BtnCancelExport_Click(object? sender, RoutedEventArgs e)
    {
        var cts = _exportCancellationTokenSource;
        if (cts is null)
        {
            return;
        }

        cts.Cancel();

        SetExportStatus("ExportCancelling", Brushes.Orange);
    }

    private HashSet<int> GetSelectedIds()
    {
        var selectedIds = new HashSet<int>(_requiredExportIds);
        if (PackageIdList.SelectedItems is null)
        {
            return selectedIds;
        }

        foreach (var item in PackageIdList.SelectedItems.OfType<PackageItem>())
        {
            selectedIds.Add(item.Id);
        }

        return selectedIds;
    }

    private void SetIndexing(bool value, string? statusKey = null)
    {
        _isIndexing = value;
        ApplyProvidedExportStatus(statusKey);
        RefreshBusyUi();
    }

    private void SetExporting(bool value, string? statusKey = null)
    {
        _isExporting = value;
        ApplyProvidedExportStatus(statusKey);
        RefreshBusyUi();
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

    private void RefreshBusyUi()
    {
        var busy = IsBusy;
        MenuOpenLogsFolder.IsEnabled = !busy;
        MenuTrends.IsEnabled = !busy && _session?.Parser.IndexTimes.Count > 0;
        BtnCancelExport.IsEnabled = _isExporting;
        CheckInputs();
    }

    private void ChkSelectAllPackagesChecked(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingSelectAll)
        {
            return;
        }

        SelectAllPackages();
        CheckInputs();
    }

    private void ChkSelectAllPackagesUnchecked(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingSelectAll)
        {
            return;
        }

        PackageIdList.SelectedItems?.Clear();
        CheckInputs();
    }

    private void SelectAllPackages()
    {
        if (PackageIdList.SelectedItems is null)
        {
            return;
        }

        _isUpdatingSelectAll = true;

        PackageIdList.SelectedItems.Clear();
        foreach (var item in _packageItems)
        {
            PackageIdList.SelectedItems.Add(item);
        }

        ChkSelectAllPackages.IsChecked = true;
        _isUpdatingSelectAll = false;

        CheckInputs();
    }

    private void OnIndexStart()
    {
        Dispatcher.UIThread.Post(() =>
        {
            SetIndexStatus("IndexingWait");
            SetIndexing(true);
        });
    }

    private void OnIndexFinish()
    {
        Dispatcher.UIThread.Post(() =>
        {
            SetIndexStatus(null);
            SetIndexing(false);
        });
    }

    private async void OpenLogsFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (IsBusy)
        {
            return;
        }

        SetIndexing(true, "SelectingLogsFolder");

        try
        {
            var selectedFolder = await SelectFolderAsync();
            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            if (!await Task.Run(() => HasLogFiles(selectedFolder)))
            {
                ResetInputFolder("NoLogFilesInFolder", Brushes.Red);
                return;
            }

            SetExportStatus(null, Brushes.Green);

            SetInputFolder(selectedFolder);
            SetDefaultOutputFolderIfEmpty(selectedFolder);
            var session = CreateSession(selectedFolder);

            _indexCancellationTokenSource?.Dispose();
            var cts = new CancellationTokenSource();
            _indexCancellationTokenSource = cts;

            try
            {
                await session.Parser.CreateOrLoadAllIndexesAsync(cts.Token);
                FillStartAndEndDateTimeFromLogsIfNeeded(session);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Indexing cancelled");
                SetIndexStatus("IndexingCancelled");
                ResetInputFolder("InputFolderNotSelected", Brushes.Gray);
            }
            finally
            {
                _indexCancellationTokenSource = null;
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
            SetIndexing(false, "");
        }
    }

    private async void OpenExportFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folder = await SelectFolderAsync();
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
            CheckInputs();
        }
    }

    private void OpenTrends_Click(object? sender, RoutedEventArgs e)
    {
        var session = _session;
        if (IsBusy || session is null || session.Parser.IndexTimes.Count == 0)
        {
            return;
        }

        var trends = new Trends(session.Parser);
        trends.Show(this);
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            title: Localizer.L("AboutTitle"),
            text: Localizer.F("AboutText", _appVersion),
            @enum: ButtonEnum.Ok);

        await box.ShowWindowDialogAsync(this);
    }

    private void ChangeLanguage_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }

        if (menuItem.Tag is not string cultureName)
        {
            return;
        }

        _logger.LogDebug("App language changed to {Lang}", cultureName);
        SetLanguage(cultureName);
    }

    private void SetLanguage(string cultureName)
    {
        LocalizationManager.Instance.SetCulture(cultureName);
        _languageSettings.SaveLanguage(cultureName);
        UpdateLanguageCheckmarks();
    }

    private void UpdateLanguageCheckmarks()
    {
        var current = LocalizationManager.Instance.CurrentCultureName;
        MenuLangRu.Icon = current == "ru" ? CreateCheckmark() : null;
        MenuLangEn.Icon = current == "en" ? CreateCheckmark() : null;
    }

    private static TextBlock CreateCheckmark()
    {
        return new TextBlock { Text = "✓" };
    }
}

internal sealed class FolderSelection
{
    public string Path { get; set; } = string.Empty;
    public bool IsSelected => !string.IsNullOrEmpty(Path);
}

// A re-localizable label: a resource key plus optional format args. Held instead of a
// formatted string so persistent labels can be re-rendered when the language changes.
internal readonly record struct StatusText(string Key, object[] Args);

internal sealed record ParserSession(LogParser Parser, CsvExport Export);