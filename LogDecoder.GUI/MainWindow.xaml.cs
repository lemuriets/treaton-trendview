using System.IO;
using LogDecoder.CAN.Packages;
using LogDecoder.GUI.Models;
using LogDecoder.Parser;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Protocol;
using LogDecoder.GUI.Services;
using LogDecoder.Helpers;
using LogDecoder.Infrastructure.Logging;
using LogDecoder.Parser.Export;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI
{
    public partial class MainWindow : Window
    {
        private bool _isUpdatingSelectAll;
        private string _selectedInputFolder = "";
        private string _selectedOutputFolder = "";
        private DateTime _startDateTime;
        private DateTime _endDateTime;
        private LogParser _logParser;
        private CsvExport _csvExport;
        private readonly ICanPackageFactory _factory;
        private readonly ILogger _logger;
        private readonly IReadOnlySet<int> _defaultSelectedIds = new HashSet<int>() { IdSynchro.Id };
        
        public MainWindow()
        {
            using var loggerProvider = new LoggerProvider();
            _logger = loggerProvider.CreateLogger<MainWindow>();
            _factory = new CanPackageFactory();
            InitializeComponent();
            FillWidgets();
            SetWindowTitle();
            ConnectEvents();
            _logger.LogInformation("GUI initialization finished");
        }

        private void SetWindowTitle()
        {
            var version = AppVersionProvider.GetVersion();
            _logger.LogInformation("Current app version: {Version}", version);
            Title = $"{Title} v{version}";
        }

        private void FillWidgets()
        {
            StartDateTime.Value = DateTime.MinValue;
            EndDateTime.Value = DateTime.Now;

            PackageIdList.ItemsSource = _factory
                .GetIdsWithNames(excludeIds: _defaultSelectedIds)
                .Select(p => new PackageItem { Id = p.Id, Name = p.Name })
                .OrderBy(p => p.Id)
                .ToList();
            
            PackageIdList.Loaded += (s, e) => PackageIdList.SelectAll();
            
            _logger.LogInformation("FillWidgets() completed");
        }

        private void ConnectEvents()
        {
            BtnSelectInputFolder.Click += SelectInputFolder_Click;
            BtnSelectOutputFolder.Click += SelectOutputFolder_Click;
            BtnExportCsv.Click += BtnExportCsvClick;
            // BtnTrendView.Click += BtnTrendView_Click;

            StartDateTime.ValueChanged += Inputs_Changed;
            EndDateTime.ValueChanged += Inputs_Changed;
            
            PackageIdList.SelectionChanged += Inputs_Changed;
            
            ChkSelectAllPackages.Checked += ChkSelectAllPackages_Checked;
            ChkSelectAllPackages.Unchecked += ChkSelectAllPackages_Unchecked;
            
            _logger.LogInformation("ConnectEvents() completed");
        }

        private async void SelectInputFolder_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtons(false);
            CheckInputs();
            
            var selectedFolder = SelectFolder();
            if (selectedFolder == "")
            {
                return;
            }
            var filesAggregator = new LogFilesAggregator(selectedFolder, Path.GetFileName, LogParser.FilenameTemplateRegex());
            if (filesAggregator.SortedFiles.Count == 0)
            {
                _selectedInputFolder = "";
                TxtSelectedInputFolder.Text = "В данной папке нет файлов с логами";
                TxtSelectedInputFolder.Foreground = Brushes.Red;
                return;
            }
            _selectedInputFolder = selectedFolder;
            if (_selectedOutputFolder == "")
            {
                _selectedOutputFolder = selectedFolder;
                
                TxtSelectedOutputFolder.Text = _selectedOutputFolder;
                TxtSelectedOutputFolder.Foreground = Brushes.Green;
            }
            TxtSelectedInputFolder.Text = _selectedInputFolder;
            TxtSelectedInputFolder.Foreground = Brushes.Green;
            _logParser = new LogParser(_logger, selectedFolder, _factory);
            _csvExport = new CsvExport(_logger, _logParser);
            
            _logParser.StartIndex += OnIndexStart;
            _logParser.FinishIndex += OnIndexFinish;

            await _logParser.CreateOrLoadAllIndexesAsync();

            if (StartDateTime.Value == DateTime.MinValue)
            {
                StartDateTime.Value = _logParser.GetStartDatetime();
            }
            CheckInputs();
        }

        private void SelectOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = SelectFolder();
            if (folder == "")
            {
                return;
            }
            _selectedOutputFolder = folder;

            TxtSelectedOutputFolder.Text = _selectedOutputFolder;
            TxtSelectedOutputFolder.Foreground = Brushes.Green;
            CheckInputs();
        }

        private string SelectFolder()
        {
            var folderDialog = new OpenFolderDialog();
            folderDialog.ShowDialog();
            return folderDialog.FolderName;
        }
        
        private void UpdateButtons(bool enabled)
        {
            // BtnTrendView.IsEnabled = enabled;
            BtnExportCsv.IsEnabled = enabled;
        }

        private void Inputs_Changed(object sender, RoutedEventArgs e)
        {
            UpdateButtons(false);
            UpdateSelectAllCheckbox();
            CheckInputs();
        }

        private void CheckInputs()
        {
            TxtErrorDateTime.Text = "";

            if (_selectedInputFolder == "" || _selectedOutputFolder == "")
            {
                return;
            }
            if (PackageIdList.SelectedItems.Count == 0)
            {
                return;
            }
            if (!StartDateTime.Value.HasValue || !EndDateTime.Value.HasValue)
            {
                return;
            }

            var start = StartDateTime.Value.Value;
            var end = EndDateTime.Value.Value;

            if (end < start)
            {
                TxtErrorDateTime.Text = "Дата конца раньше даты начала!";
                return;
            }

            _startDateTime = start;
            _endDateTime = end;
            UpdateButtons(true);
        }
        
        private void UpdateSelectAllCheckbox()
        {
            _isUpdatingSelectAll = true;

            ChkSelectAllPackages.IsChecked = PackageIdList.SelectedItems.Count == PackageIdList.Items.Count;

            _isUpdatingSelectAll = false;
        }

        private async void BtnExportCsvClick(object sender, RoutedEventArgs e)
        {
            BtnExportCsv.IsEnabled = false;

            var selectedIds = PackageIdList.SelectedItems
                .Cast<PackageItem>()
                .Select(p => p.Id)
                .ToHashSet();
            
            selectedIds.UnionWith(_defaultSelectedIds);

            var inputFolder = _selectedInputFolder;
            var outputFolder = _selectedOutputFolder;
            var start = _startDateTime;
            var end = _endDateTime;

            var ignoreDuplicates = ChkIgnoreDuplicates.IsChecked == true;
            var excludeEmptyTimestamps = ChkExcludeEmptyTimestamps.IsChecked == true;

            try
            {
                TxtExportStatus.Text = "Экспорт журнала ошибок. Подождите...";
                TxtExportStatus.Foreground = Brushes.Green;

                await Task.Run(() =>
                {
                    _csvExport.ToCsv(
                        inputFolder,
                        outputFolder,
                        selectedIds,
                        start,
                        end,
                        new HashSet<PackageTechStatus>{
                            PackageTechStatus.Warning,
                            PackageTechStatus.Error,
                            PackageTechStatus.Critical,
                            PackageTechStatus.Info,
                            PackageTechStatus.Ok
                        },
                        ignoreDuplicates,
                        excludeEmptyTimestamps);
                });

                TxtExportStatus.Text = "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while parsing IVL logs");
                TxtExportStatus.Text = "Ошибка: " + ex.Message;
                TxtExportStatus.Foreground = Brushes.Red;
            }
            finally
            {
                BtnExportCsv.IsEnabled = true;
            }
        }
        
        private void BtnTrendView_Click(object sender, RoutedEventArgs e)
        {
            var trendWindow = new TrendView(_logParser)
            {
                Owner = this
            };
            trendWindow.Show();
        }
        
        private void ChkSelectAllPackages_Checked(object sender, RoutedEventArgs e)
        {
            PackageIdList.SelectAll();
        }

        private void ChkSelectAllPackages_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAll)
            {
                return;
            }
            PackageIdList.UnselectAll();
        }

        private void OnIndexStart()
        {
            TxtIndexStatus.Text = "Индексирование... Подождите";
            StartDateTime.IsEnabled = false;
            EndDateTime.IsEnabled = false;
            BtnExportCsv.IsEnabled = false;
        }

        private void OnIndexFinish()
        {
            TxtIndexStatus.Text = "";
            StartDateTime.IsEnabled = true;
            EndDateTime.IsEnabled = true;
            BtnExportCsv.IsEnabled = true;
        }
    }
}