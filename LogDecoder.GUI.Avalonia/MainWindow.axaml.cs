using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
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

    // Parameterless ctor required by the Avalonia XAML compiler/previewer; at
    // runtime App always uses the factory ctor with the loaded protocol.
    public MainWindow() : this(new CanPackageFactory(), new LoggerProvider())
    {
    }

    public MainWindow(CanPackageFactory factory, LoggerProvider loggerProvider)
    {
        _loggerProvider = loggerProvider;
        var logger = _loggerProvider.CreateLogger<MainWindow>();

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
        PackageIdList.AddHandler(PointerPressedEvent, PackageListPointerPressed, RoutingStrategies.Tunnel);
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
        PackageIdList.RemoveHandler(PointerPressedEvent, PackageListPointerPressed);
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

    private void PackageListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(PackageIdList).Properties.IsRightButtonPressed)
        {
            return;
        }

        // Right-click is for showing the package description, not selecting.
        // Handle it here so the ListBox does not toggle the current selection.
        e.Handled = true;

        if (e.Source is Control source)
        {
            ShowDescriptionFlyout(source);
        }
    }

    private void ShowDescriptionFlyout(Control source)
    {
        if (_descriptionFlyout is null || source.DataContext is not PackageItem item)
        {
            return;
        }

        var target = source.FindAncestorOfType<ListBoxItem>() ?? source;
        if (_descriptionFlyout.Content is Control content)
        {
            content.DataContext = item;
        }

        _descriptionFlyout.ShowAt(target, showAtPointer: true);
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
