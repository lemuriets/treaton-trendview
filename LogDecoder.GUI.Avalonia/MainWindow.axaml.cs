using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LogDecoder.CAN.Packages;
using LogDecoder.GUI.Avalonia.Models;
using LogDecoder.GUI.Avalonia.Services;
using LogDecoder.GUI.Avalonia.ViewModels;
using LogDecoder.Helpers;
using LogDecoder.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI.Avalonia;

public partial class MainWindow : Window
{
    private readonly LoggerProvider _loggerProvider;
    private readonly MainWindowViewModel _viewModel;

    private bool _isUpdatingSelectAll;
    private Flyout? _descriptionFlyout;

    public MainWindow()
    {
        _loggerProvider = new LoggerProvider();
        var logger = _loggerProvider.CreateLogger<MainWindow>();

        var factory = new CanPackageFactory();
        var packageCatalog = new PackageCatalog(factory);
        var indexing = new IndexingService(logger, factory);
        var export = new ExportService();
        var logFolders = new LogFolderService();
        var language = new LanguageService(new LanguageSettingsService(logger));
        var folderPicker = new FolderPickerService(() => TopLevel.GetTopLevel(this));
        var folderLauncher = new FolderLauncherService(logger);
        var dialogs = new DialogService(() => this);

        _viewModel = new MainWindowViewModel(
            logger,
            packageCatalog,
            indexing,
            export,
            logFolders,
            language,
            folderPicker,
            folderLauncher,
            dialogs,
            parser => new Trends(parser).Show(this),
            () => new DebugLogWindow().Show(this),
            AppVersionProvider.GetVersion());

        InitializeComponent();

        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        PackageIdList.SelectionChanged += PackageSelectionChanged;
        PackageIdList.ContextRequested += PackageContextRequested;
        ChkSelectAllPackages.Checked += ChkSelectAllPackagesChecked;
        ChkSelectAllPackages.Unchecked += ChkSelectAllPackagesUnchecked;

        _descriptionFlyout = (Flyout)Resources["PackageDescriptionFlyout"]!;

        UpdateLanguageCheckmarks();

        logger.LogDebug("GUI initialization finished");
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        PackageIdList.SelectionChanged -= PackageSelectionChanged;
        PackageIdList.ContextRequested -= PackageContextRequested;
        ChkSelectAllPackages.Checked -= ChkSelectAllPackagesChecked;
        ChkSelectAllPackages.Unchecked -= ChkSelectAllPackagesUnchecked;

        _viewModel.Dispose();
        _loggerProvider.Dispose();

        base.OnClosed(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentCulture))
        {
            UpdateLanguageCheckmarks();
        }
    }

    private void PackageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var ids = PackageIdList.SelectedItems?
            .OfType<PackageItem>()
            .Select(p => p.Id)
            .ToHashSet() ?? [];

        _viewModel.SetSelectedPackageIds(ids);
        UpdateSelectAllCheckbox();
    }

    private void PackageContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (_descriptionFlyout is null || e.Source is not Control source)
        {
            return;
        }

        if (source.DataContext is not PackageItem item)
        {
            return;
        }

        var target = source.FindAncestorOfType<ListBoxItem>() ?? source;
        if (_descriptionFlyout.Content is Control content)
        {
            content.DataContext = item;
        }

        _descriptionFlyout.ShowAt(target, showAtPointer: true);
        e.Handled = true;
    }

    private void ChkSelectAllPackagesChecked(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingSelectAll)
        {
            return;
        }

        SelectAllPackages();
    }

    private void ChkSelectAllPackagesUnchecked(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingSelectAll)
        {
            return;
        }

        PackageIdList.SelectedItems?.Clear();
    }

    private void SelectAllPackages()
    {
        if (PackageIdList.SelectedItems is null)
        {
            return;
        }

        _isUpdatingSelectAll = true;

        PackageIdList.SelectedItems.Clear();
        foreach (var item in _viewModel.Packages)
        {
            PackageIdList.SelectedItems.Add(item);
        }

        ChkSelectAllPackages.IsChecked = true;
        _isUpdatingSelectAll = false;
    }

    private void UpdateSelectAllCheckbox()
    {
        if (_isUpdatingSelectAll)
        {
            return;
        }

        _isUpdatingSelectAll = true;
        ChkSelectAllPackages.IsChecked = PackageIdList.SelectedItems?.Count == _viewModel.Packages.Count;
        _isUpdatingSelectAll = false;
    }

    private void UpdateLanguageCheckmarks()
    {
        var current = _viewModel.CurrentCulture;
        MenuLangRu.Icon = current == "ru" ? CreateCheckmark() : null;
        MenuLangEn.Icon = current == "en" ? CreateCheckmark() : null;
    }

    private static TextBlock CreateCheckmark()
    {
        return new TextBlock { Text = "●" };
    }
}
