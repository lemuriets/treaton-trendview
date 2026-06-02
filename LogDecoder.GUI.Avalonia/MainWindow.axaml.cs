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

    private readonly FolderSelection _inputFolder = new();
    private readonly FolderSelection _outputFolder = new();
    private ParserSession? _session;
    private List<PackageItem> _packageItems = [];
    private CancellationTokenSource? _exportCancellationTokenSource;
    private Task? _exportTask;
    private CancellationTokenSource? _indexCancellationTokenSource;

    public MainWindow()
    {
        _loggerProvider = new LoggerProvider();
        _logger = _loggerProvider.CreateLogger<MainWindow>();
        _factory = new CanPackageFactory();

        InitializeComponent();

        DataContext = LocalizationManager.Instance;
        LocalizationManager.Instance.PropertyChanged += LocalizationChanged;

        FillWidgets();
        SetWindowTitle();
        ConnectEvents();

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
        if (!_inputFolder.IsSelected)
        {
            TxtSelectedInputFolder.Text = Localizer.L("InputFolderNotSelected");
            TxtSelectedInputFolder.Foreground = Brushes.Gray;
            SetFolderLinkState(TxtSelectedInputFolder, false);
        }

        if (!_outputFolder.IsSelected)
        {
            TxtSelectedOutputFolder.Text = Localizer.L("OutputFolderNotSelected");
            TxtSelectedOutputFolder.Foreground = Brushes.Gray;
            SetFolderLinkState(TxtSelectedOutputFolder, false);
        }
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
        SetFolder(_inputFolder, TxtSelectedInputFolder, folder);
    }

    private void ResetInputFolder(string message, IBrush foreground)
    {
        ResetFolder(_inputFolder, TxtSelectedInputFolder, message, foreground);

        UnsubscribeParserEvents(_session);
        _session = null;
    }

    private void SetOutputFolder(string folder)
    {
        SetFolder(_outputFolder, TxtSelectedOutputFolder, folder);
    }

    private void ResetOutputFolder(string message, IBrush foreground)
    {
        ResetFolder(_outputFolder, TxtSelectedOutputFolder, message, foreground);
    }
    
    private void SetFolder(FolderSelection selection, TextBlock textBlock, string folder)
    {
        selection.Path = folder;

        textBlock.Text = folder;
        textBlock.Foreground = Brushes.Green;

        SetFolderLinkState(textBlock, true);
    }

    private void ResetFolder(FolderSelection selection, TextBlock textBlock, string message, IBrush foreground)
    {
        selection.Path = string.Empty;

        textBlock.Text = message;
        textBlock.Foreground = foreground;

        SetFolderLinkState(textBlock, false);
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

    private async Task OpenFolderAsync(FolderSelection folderSelection)
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

        try {
            var launcher = TopLevel.GetTopLevel(this)?.Launcher;
            if (launcher is null)
            {
                _logger.LogWarning("Launcher is not available");
                return;
            }

            if (!Uri.TryCreate(folderSelection.Path, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Cannot build Uri from folder path: {Folder}", folderSelection.Path);
                return;
            }

            await launcher.LaunchUriAsync(uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed OpenFolderAsync({Folder})", folderSelection.Path);
        }
    }

    private async void SelectedInputFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            await OpenFolderAsync(_inputFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in SelectedInputFolder_Click");
        }
    }

    private async void SelectedOutputFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            await OpenFolderAsync(_outputFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in SelectedOutputFolder_Click");
        }
    }

    private void SetDefaultOutputFolderIfEmpty(string folder)
    {
        if (!string.IsNullOrEmpty(_outputFolder.Path))
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
        TxtExportStatus.Text = "";
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

        if (string.IsNullOrEmpty(_inputFolder.Path) || string.IsNullOrEmpty(_outputFolder.Path))
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
        var inputFolder = _inputFolder.Path;
        var outputFolder = _outputFolder.Path;
        var ignoreDuplicates = ChkIgnoreDuplicates.IsChecked == true;
        var excludeEmptyTimestamps = ChkExcludeEmptyTimestamps.IsChecked == true;

        _exportCancellationTokenSource?.Dispose();
        var cts = new CancellationTokenSource();
        _exportCancellationTokenSource = cts;

        var cancellationToken = cts.Token;

        SetExporting(true, Localizer.L("ExportingWait"));

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

            TxtExportStatus.Text = Localizer.L("ExportSuccess");
            TxtExportStatus.Foreground = Brushes.Green;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("CSV export cancelled");

            TxtExportStatus.Text = Localizer.L("ExportCancelled");
            TxtExportStatus.Foreground = Brushes.Orange;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing IVL logs");

            TxtExportStatus.Text = Localizer.F("ExportError", ex.Message);
            TxtExportStatus.Foreground = Brushes.Red;
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

        TxtExportStatus.Text = Localizer.L("ExportCancelling");
        TxtExportStatus.Foreground = Brushes.Orange;
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

    private void SetIndexing(bool value, string? status = null)
    {
        _isIndexing = value;
        SetExportStatusIfProvided(status);
        RefreshBusyUi();
    }

    private void SetExporting(bool value, string? status = null)
    {
        _isExporting = value;
        SetExportStatusIfProvided(status);
        RefreshBusyUi();
    }

    private void SetExportStatusIfProvided(string? status)
    {
        if (status is null)
        {
            return;
        }
        TxtExportStatus.Text = status;
        TxtExportStatus.Foreground = Brushes.Green;
    }

    private void RefreshBusyUi()
    {
        var busy = IsBusy;
        MenuOpenLogsFolder.IsEnabled = !busy;
        MenuOpenExportFolder.IsEnabled = !busy;
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
            TxtIndexStatus.Text = Localizer.L("IndexingWait");
            SetIndexing(true);
        });
    }

    private void OnIndexFinish()
    {
        Dispatcher.UIThread.Post(() =>
        {
            TxtIndexStatus.Text = string.Empty;
            SetIndexing(false);
        });
    }

    private async void OpenLogsFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (IsBusy)
        {
            return;
        }

        SetIndexing(true, Localizer.L("SelectingLogsFolder"));

        try
        {
            var selectedFolder = await SelectFolderAsync();
            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            if (!await Task.Run(() => HasLogFiles(selectedFolder)))
            {
                ResetInputFolder(Localizer.L("NoLogFilesInFolder"), Brushes.Red);
                return;
            }

            TxtExportStatus.Text = "";

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
                TxtIndexStatus.Text = Localizer.L("IndexingCancelled");
                ResetInputFolder(Localizer.L("InputFolderNotSelected"), Brushes.Gray);
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
            ResetInputFolder(Localizer.F("InputFolderSelectionError", ex.Message), Brushes.Red);
        }
        finally
        {
            SetIndexing(false, "");
        }
    }

    private async void OpenExportFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (IsBusy)
        {
            return;
        }

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

            ResetOutputFolder(Localizer.F("OutputFolderSelectionError", ex.Message), Brushes.Red);
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
    }
}

internal sealed class FolderSelection
{
    public string Path { get; set; } = string.Empty;
    public bool IsSelected => !string.IsNullOrEmpty(Path);
}

internal sealed record ParserSession(LogParser Parser, CsvExport Export);