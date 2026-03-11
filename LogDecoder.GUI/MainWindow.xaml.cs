using System.IO;
using LogDecoder.CAN.Packages;
using LogDecoder.GUI.Models;
using LogDecoder.Parser;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using LogDecoder.CAN.Contracts;
using LogDecoder.GUI.Services;
using LogDecoder.Parser.Export;

namespace LogDecoder.GUI
{
    public partial class MainWindow : Window
    {
        private string _selectedInputFolder = "";
        private string _selectedOutputFolder = "";
        private DateTime _startDateTime;
        private DateTime _endDateTime;
        private LogParser _logParser;
        private ExcelExport _excelExport;
        private readonly ICanPackageFactory _factory;
        
        public MainWindow()
        {
            _factory = new CanPackageFactory();
            InitializeComponent();
            FillWidgets();
            ConnectEvents();
        }

        private void FillWidgets()
        {
            StartDateTime.Value = DateTime.Now;
            EndDateTime.Value = DateTime.Now;

            PackageIdList.ItemsSource = _factory
                .GetIdsWithNames()
                .Select(p => new PackageItem { Id = p.Id, Name = p.Name })
                .ToList();
        }

        private void ConnectEvents()
        {
            BtnSelectInputFolder.Click += SelectInputFolder_Click;
            BtnSelectOutputFolder.Click += SelectOutputFolder_Click;
            BtnExportExcel.Click += BtnExportExcel_Click;
            BtnTrendView.Click += BtnTrendView_Click;

            StartDateTime.ValueChanged += Inputs_Changed;
            EndDateTime.ValueChanged += Inputs_Changed;
            
            PackageIdList.SelectionChanged += Inputs_Changed;
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
            TxtSelectedInputFolder.Text = _selectedInputFolder;
            TxtSelectedInputFolder.Foreground = Brushes.Green;
            _logParser = new LogParser(selectedFolder, _factory);
            _excelExport = new ExcelExport(_logParser);
            
            _logParser.StartIndex += OnIndexStart;
            _logParser.FinishIndex += OnIndexFinish;

            await _logParser.CreateOrLoadAllIndexesAsync();
            
            StartDateTime.Value = _logParser.GetStartDatetime();

            CheckInputs();
        }

        private void SelectOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            _selectedOutputFolder = SelectFolder();

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
            BtnTrendView.IsEnabled = enabled;
            BtnExportExcel.IsEnabled = enabled;
        }

        private void Inputs_Changed(object sender, RoutedEventArgs e)
        {
            UpdateButtons(false);
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

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            BtnExportExcel.IsEnabled = false;

            var selectedIds = PackageIdList.SelectedItems
                .Cast<PackageItem>()
                .Select(p => p.Id)
                .ToHashSet();
            var inputFolder = _selectedInputFolder;
            var outputFolder = _selectedOutputFolder;
            var start = _startDateTime;
            var end = _endDateTime;

            try
            {
                TxtExportStatus.Text = "Экспорт журнала ошибок. Подождите...";
                TxtExportStatus.Foreground = Brushes.Green;
                await Task.Run(() =>
                {
                    _excelExport.ToExcel(inputFolder, outputFolder, selectedIds, start, end);
                });

                TxtExportStatus.Text = "";
            }
            catch (Exception ex)
            {
                TxtExportStatus.Text = "Ошибка: " + ex.Message;
                TxtExportStatus.Foreground = Brushes.Red;
            }
            finally
            {
                BtnExportExcel.IsEnabled = true;
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

        private void OnIndexStart()
        {
            TxtIndexStatus.Text = "Индексирование... Подождите";
            StartDateTime.IsEnabled = false;
            EndDateTime.IsEnabled = false;
            CheckInputs();
        }

        private void OnIndexFinish()
        {
            TxtIndexStatus.Text = "";
            StartDateTime.IsEnabled = true;
            EndDateTime.IsEnabled = true;
            CheckInputs();
        }
    }
}