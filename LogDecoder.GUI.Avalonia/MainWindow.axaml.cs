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
    private readonly IReadOnlySet<int> _requiredExportIds = new HashSet<int> { IdSynchro.Id };

    private bool _isBusy;
    private bool _isUpdatingSelectAll;
    private readonly FolderSelection _inputFolder = new();
    private readonly FolderSelection _outputFolder = new();
    private DateTime _startDateTime;
    private DateTime _endDateTime;
    private LogParser? _logParser;
    private CsvExport? _csvExport;
    private List<PackageItem> _packageItems = [];
    private CancellationTokenSource? _exportCancellationTokenSource;

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

        _logger.LogInformation("GUI initialization finished");
    }

    protected override void OnClosed(EventArgs e)
    {
        LocalizationManager.Instance.PropertyChanged -= LocalizationChanged;
        UnsubscribeParserEvents();
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
        var version = AppVersionProvider.GetVersion();

        _logger.LogInformation("Current app version: {Version}", version);

        Title = $"{AppName} v{version}";
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

        _logger.LogInformation("FillWidgets() completed");
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

        _logger.LogInformation("ConnectEvents() completed");
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

        _logParser = null;
        _csvExport = null;
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
        selection.IsSelected = true;

        textBlock.Text = folder;
        textBlock.Foreground = Brushes.Green;

        SetFolderLinkState(textBlock, true);
    }

    private void ResetFolder(FolderSelection selection, TextBlock textBlock, string message, IBrush foreground)
    {
        selection.Path = string.Empty;
        selection.IsSelected = false;

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
            var uri = new Uri(folderSelection.Path);

            await launcher.LaunchUriAsync(uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed OpenFolderAsync({Folder})", folderSelection.Path);
        }
    }

    private async void SelectedInputFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        await OpenFolderAsync(_inputFolder);
    }

    private async void SelectedOutputFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        await OpenFolderAsync(_outputFolder);
    }

    private void SetDefaultOutputFolderIfEmpty(string folder)
    {
        if (!string.IsNullOrEmpty(_outputFolder.Path))
        {
            return;
        }
        SetOutputFolder(folder);
    }

    private void CreateParser(string selectedFolder)
    {
        UnsubscribeParserEvents();

        _logParser = new LogParser(_logger, selectedFolder, _factory);
        _csvExport = new CsvExport(_logger, _logParser);

        _logParser.StartIndex += OnIndexStart;
        _logParser.FinishIndex += OnIndexFinish;
    }

    private void UnsubscribeParserEvents()
    {
        if (_logParser is null)
        {
            return;
        }

        _logParser.StartIndex -= OnIndexStart;
        _logParser.FinishIndex -= OnIndexFinish;
    }

    private void FillStartDateTimeFromLogsIfNeeded()
    {
        if (_logParser is null)
        {
            return;
        }

        if (!TryGetDateTime(StartDateTime.Text, out var currentStart) || currentStart != DateTime.MinValue)
        {
            return;
        }

        var start = _logParser.MinDatetime;
        if (start.HasValue)
        {
            StartDateTime.Text = start.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }
    }

    private void InputsChanged(object? sender, EventArgs e)
    {
        UpdateSelectAllCheckbox();
        CheckInputs();
    }

    private void CheckInputs()
    {
        TxtErrorDateTime.Text = string.Empty;
        BtnExportCsv.IsEnabled = CanExport();
    }

    private bool CanExport()
    {
        if (!TryGetDateTime(StartDateTime.Text, out var start) || !TryGetDateTime(EndDateTime.Text, out var end))
        {
            TxtErrorDateTime.Text = Localizer.F("InvalidDateFormat", DateTimeFormat);
            return false;
        }

        if (end < start)
        {
            TxtErrorDateTime.Text = Localizer.L("EndBeforeStart");
            return false;
        }
        
        if (_isBusy)
        {
            return false;
        }

        if (_csvExport is null)
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
        
        _startDateTime = start;
        _endDateTime = end;

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
        if (_csvExport is null || !CanExport())
        {
            CheckInputs();
            return;
        }

        var selectedIds = GetSelectedIds();
        var inputFolder = _inputFolder.Path;
        var outputFolder = _outputFolder.Path;
        var start = _startDateTime;
        var end = _endDateTime;
        var ignoreDuplicates = ChkIgnoreDuplicates.IsChecked == true;
        var excludeEmptyTimestamps = ChkExcludeEmptyTimestamps.IsChecked == true;

        _exportCancellationTokenSource?.Dispose();
        _exportCancellationTokenSource = new CancellationTokenSource();

        var cancellationToken = _exportCancellationTokenSource.Token;

        SetBusy(true, Localizer.L("ExportingWait"), canCancel: true);

        try
        {
            await Task.Run(() =>
            {
                _csvExport.ToCsv(
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

            TxtExportStatus.Text = Localizer.L("ExportSuccess");
            TxtExportStatus.Foreground = Brushes.Green;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CSV export cancelled");

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
            _exportCancellationTokenSource.Dispose();
            _exportCancellationTokenSource = null;

            SetBusy(false);
            CheckInputs();
        }
    }
    
    private void BtnCancelExport_Click(object? sender, RoutedEventArgs e)
    {
        _exportCancellationTokenSource?.Cancel();

        TxtExportStatus.Text = Localizer.L("ExportCancelling");
        TxtExportStatus.Foreground = Brushes.Orange;
    }

    private HashSet<int> GetSelectedIds()
    {
        var selectedIds = PackageIdList.SelectedItems!
            .Cast<PackageItem>()
            .Select(p => p.Id)
            .ToHashSet();

        selectedIds.UnionWith(_requiredExportIds);

        return selectedIds;
    }

    private void SetBusy(bool isBusy, string? status = null, bool canCancel = false)
    {
        _isBusy = isBusy;

        MenuOpenLogsFolder.IsEnabled = !isBusy;
        MenuOpenExportFolder.IsEnabled = !isBusy;

        BtnCancelExport.IsEnabled = isBusy && canCancel;

        if (status is not null)
        {
            TxtExportStatus.Text = status;
            TxtExportStatus.Foreground = Brushes.Green;
        }

        BtnExportCsv.IsEnabled = !isBusy && CanExport();
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
            SetBusy(true);
            CheckInputs();
        });
    }

    private void OnIndexFinish()
    {
        Dispatcher.UIThread.Post(() =>
        {
            TxtIndexStatus.Text = string.Empty;
            SetBusy(false);
            CheckInputs();
        });
    }

    private async void OpenLogsFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        SetBusy(true, Localizer.L("SelectingLogsFolder"));

        try
        {
            var selectedFolder = await SelectFolderAsync();
            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            if (!HasLogFiles(selectedFolder))
            {
                ResetInputFolder(Localizer.L("NoLogFilesInFolder"), Brushes.Red);
                return;
            }

            TxtExportStatus.Text = "";
            TxtExportStatus.Foreground = Brushes.Green;
            
            SetInputFolder(selectedFolder);
            SetDefaultOutputFolderIfEmpty(selectedFolder);
            CreateParser(selectedFolder);

            await _logParser!.CreateOrLoadAllIndexesAsync();
            FillStartDateTimeFromLogsIfNeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while selecting input folder");
            ResetInputFolder(Localizer.F("InputFolderSelectionError", ex.Message), Brushes.Red);
        }
        finally
        {
            SetBusy(false, "");
            CheckInputs();
        }
    }

    private async void OpenExportFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_isBusy)
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

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            title: Localizer.L("AboutTitle"),
            text: Localizer.F("AboutText", AppVersionProvider.GetVersion()),
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

        _logger.LogInformation("App language changed to {Lang}", cultureName);
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
    public bool IsSelected { get; set; }
}