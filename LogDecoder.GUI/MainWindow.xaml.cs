using System.DirectoryServices.ActiveDirectory;
using LogDecoder.CAN.Packages;
using LogDecoder.GUI.Models;
using LogDecoder.Parser;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using LogDecoder.Parser.Export;

namespace LogDecoder.GUI
{
    public partial class MainWindow : Window
    {
        private string selectedInputFolder = "";
        private string selectedOutputFolder = "";
        private DateTime startDateTime;
        private DateTime endDateTime;
        private LogParser logParser;
        private ExcelExport _excelExport;
        private string[] binFiles;
        private bool _indexing;

        public MainWindow()
        {
            InitializeComponent();
            FillWidgets();
            ConnectEvents();
        }

        private void FillWidgets()
        {
            StartDateTime.Value = DateTime.Now;
            EndDateTime.Value = DateTime.Now;

            PackageIdList.ItemsSource = CanPackageFactory
                .GetIdsWithNames()
                .Select(p => new PackageItem { Id = p.Id, Name = p.PackageName })
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
            UpdateButtonsAfterSelectFolder(false);
            CheckInputs();
            
            var selectedFolder = SelectFolder();
            if (selectedFolder == "")
            {
                return;
            }
            binFiles = LogParser.FindBinFilesSorted(selectedFolder);
            if (binFiles.Length == 0)
            {
                selectedInputFolder = "";
                TxtSelectedInputFolder.Text = "В данной папке нет файлов с логами";
                TxtSelectedInputFolder.Foreground = Brushes.Red;
                return;
            }

            selectedInputFolder = selectedFolder;
            TxtSelectedInputFolder.Text = selectedInputFolder;
            TxtSelectedInputFolder.Foreground = Brushes.Green;
            logParser = new LogParser(selectedFolder);
            _excelExport = new ExcelExport(logParser);
            
            logParser.StartIndex += OnIndexStart;
            logParser.FinishIndex += OnIndexFinish;

            await logParser.CreateAndLoadAllIndexesAsync();
            
            StartDateTime.Value = logParser.GetStartDatetime();

            UpdateButtonsAfterSelectFolder(true);
            CheckInputs();
        }

        private void SelectOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            selectedOutputFolder = SelectFolder();

            TxtSelectedOutputFolder.Text = selectedOutputFolder;
            TxtSelectedOutputFolder.Foreground = Brushes.Green;
            CheckInputs();
        }

        private string SelectFolder()
        {
            var folderDialog = new OpenFolderDialog();
            folderDialog.ShowDialog();
            return folderDialog.FolderName;
        }
        
        private void UpdateButtonsAfterSelectFolder(bool enabled)
        {
            BtnTrendView.IsEnabled = enabled;
        }

        private void Inputs_Changed(object sender, RoutedEventArgs e)
        {
            UpdateButtonsAfterSelectFolder(false);
            CheckInputs();
        }

        private void CheckInputs()
        {
            BtnExportExcel.IsEnabled = false;
            TxtErrorDateTime.Text = "";

            if (selectedInputFolder == "" || selectedOutputFolder == "")
                return;

            if (PackageIdList.SelectedItems.Count == 0)
                return;

            if (!StartDateTime.Value.HasValue || !EndDateTime.Value.HasValue)
                return;

            if (_indexing)
                return;

            var start = StartDateTime.Value.Value;
            var end = EndDateTime.Value.Value;

            if (end < start)
            {
                TxtErrorDateTime.Text = "Дата конца раньше даты начала!";
                return;
            }

            startDateTime = start;
            endDateTime = end;

            BtnExportExcel.IsEnabled = true;
        }

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            BtnExportExcel.IsEnabled = false;

            var selectedIds = PackageIdList.SelectedItems
                .Cast<PackageItem>()
                .Select(p => p.Id)
                .ToHashSet();
            var title = "Данные пакетов";
            var outputFolder = selectedOutputFolder;
            var start = startDateTime;
            var end = endDateTime;
            
            Console.WriteLine(outputFolder);
            
            Console.WriteLine(string.Join(' ', selectedIds));

            try
            {
                TxtExportStatus.Text = "Производится экспорт. Подождите";
                await Task.Run(() =>
                {
                    _excelExport.ToExcel(title, outputFolder, selectedIds, start, end);
                });
            }
            catch (Exception ex)
            {
                TxtExportStatus.Text = "Ошибка: " + ex.Message;
            }
            finally
            {
                BtnExportExcel.IsEnabled = true;
                TxtExportStatus.Text = "";
            }
        }
        
        private void BtnTrendView_Click(object sender, RoutedEventArgs e)
        {
            var trendWindow = new TrendView(logParser)
            {
                Owner = this
            };
            trendWindow.Show();
        }

        private void OnIndexStart()
        {
            TxtIndexStatus.Text = "Индексирование... Подождите";
            BtnTrendView.IsEnabled = false;
            _indexing = true;
            
            CheckInputs();
        }

        private void OnIndexFinish()
        {
            TxtIndexStatus.Text = "";
            BtnTrendView.IsEnabled = true;
            _indexing = false;
            
            CheckInputs();
        }
    }
}