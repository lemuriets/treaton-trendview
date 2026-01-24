using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LogDecoder.CAN.General;
using LogDecoder.Parser;
using LogDecoder.CAN.Packages;
using LogDecoder.GUI.Commands;
using LogDecoder.GUI.Models;
using LogDecoder.GUI.Services;
using OxyPlot;

namespace LogDecoder.GUI.ViewModels;

internal class Cursor
{
    public double X { get; set; }
    public DateTime CurrentDateTime { get; set; }
}

public class TrendViewModel : INotifyPropertyChanged
{
    public TrendViewModel(LogParser parser)
    {
        _dataProvider = new DataProvider(parser);
        
        _scale = Scales[0];
        
        PlotBox1 = new PlotBoxViewModel("", 0, _scale.Seconds, OxyColors.Lime);
        PlotBox2 = new PlotBoxViewModel("", 0, _scale.Seconds, OxyColors.Red);
        PlotBox3 = new PlotBoxViewModel("", 0, _scale.Seconds, OxyColors.Blue);

        _plots = [ PlotBox1, PlotBox2, PlotBox3 ];
        
        SetupPlots();
        ApplyScales();
        
        CurrentDateTime = StartDateTime;
        
        MoveCursorsLeftCommand = new RelayCommand(_ => MoveCursorsLeft());
        MoveCursorsRightCommand = new RelayCommand(_ => MoveCursorsRight());
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ICommand MoveCursorsLeftCommand { get; }
    public ICommand MoveCursorsRightCommand { get; }
    
    public PlotBoxViewModel PlotBox1 { get; }
    public PlotBoxViewModel PlotBox2 { get; }
    public PlotBoxViewModel PlotBox3 { get; }
        
    public ScaleItem[] Scales { get; } = [ new(5), new(10), new(15), new(30), new(60), new(120), new(180), new(240), new(300), new(600) ];
    
    private ScaleItem _scale;
    public ScaleItem Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            ApplyScales();
        }
    }
    
    private string _txtError = string.Empty;
    public string TxtError
    {
        get => _txtError;
        set
        {
            _txtError = value;
            OnPropertyChanged(nameof(TxtError));
        }
    }

    private ObservableCollection<LogMessage> _messages = [];
    public ObservableCollection<LogMessage> Messages 
    { 
        get => _messages;
        set
        {
            _messages = value;
            OnPropertyChanged(nameof(Messages));
        } 
    }
    
    public TrendsData AllSeries { get; set; } = new();

    public double ValBattery => AllSeries.Battery.GetValueAtIndexSafe((int)_cursorX);
    public double ValPIP => AllSeries.PIP.GetValueAtIndexSafe((int)_cursorX);
    public double ValPEEP => AllSeries.PEEP.GetValueAtIndexSafe((int)_cursorX);
    public double ValMVTotal => AllSeries.MVTotal.GetValueAtIndexSafe((int)_cursorX);
    public double ValMVMachine => AllSeries.MVMachine.GetValueAtIndexSafe((int)_cursorX);
    public double ValVt => AllSeries.Vt.GetValueAtIndexSafe((int)_cursorX);
    public double ValRB1 => AllSeries.RB1.GetValueAtIndexSafe((int)_cursorX);
    public double ValRB2 => AllSeries.RB2.GetValueAtIndexSafe((int)_cursorX);
    public double ValCst => AllSeries.Cst.GetValueAtIndexSafe((int)_cursorX);
    public double ValRst => AllSeries.Rst.GetValueAtIndexSafe((int)_cursorX);
    public double ValLeak => AllSeries.Leak.GetValueAtIndexSafe((int)_cursorX);
    
    private DateTime _startDateTime;
    public DateTime StartDateTime
    {
        get => _startDateTime;
        set
        {
            var index = (value.ToString());
            if (index == -1)
            {
                TxtError = "Дата не найдена!";
                return;
            }
            _startDateTime = value;
            OnPropertyChanged(nameof(StartDateTime));
            
            TxtError = "";

            UpdateCursorsAndValues(index);
            UpdatePlots(value, _scale.Seconds);
        }
    }
    
    private DateTime _currentDateTime;
    public DateTime CurrentDateTime
    {
        get => _currentDateTime;
        set
        {
            _currentDateTime = value;
            OnPropertyChanged(nameof(CurrentDateTime));
        }
    }
    
    private double _cursorX;
    public double CursorX
    {
        get => _cursorX;
        set
        {
            _cursorX = value;
            UpdateCurrentValues();
        }
    }
    
    private readonly PlotBoxViewModel[] _plots;
    private readonly DataProvider _dataProvider;
    
    private void SetupPlots()
    {
        PlotBox1.SelectedPlot = AllSeries.Flow;
        PlotBox2.SelectedPlot = AllSeries.Paw;
        PlotBox3.SelectedPlot = AllSeries.Vol;
        
        ApplyToAllPlots(plot => plot.PlotChanged += OnPlotBoxChanged);
        ApplyToAllPlots(plot => plot.AvailablePlots = AllSeries.AllSeries);
    }
    
    private void UpdateCurrentValues()
    {
        OnPropertyChanged(nameof(ValBattery));
        OnPropertyChanged(nameof(ValPIP));
        OnPropertyChanged(nameof(ValPEEP));
        OnPropertyChanged(nameof(ValMVTotal));
        OnPropertyChanged(nameof(ValMVMachine));
        OnPropertyChanged(nameof(ValVt));
        OnPropertyChanged(nameof(ValRB1));
        OnPropertyChanged(nameof(ValRB2));
        OnPropertyChanged(nameof(ValCst));
        OnPropertyChanged(nameof(ValRst));
        OnPropertyChanged(nameof(ValLeak));
    }
    
    private void UpdatePlots(DateTime start, int lenghtSec)
    {
        AllSeries.ClearAll();
        Messages.Clear();
        
        _dataProvider.GetDataForTimeSpan(AllSeries, Messages, start, lenghtSec, PlotBox1.XMin);
        
        ApplyToAllPlots(plot => plot.LoadNewPointsAndUpdate(plot.SelectedPlot.Points));
    }
    
    private DateTime GetDateTimeByX(double x)
    {
        return DateTime.Parse(_allIndexes[(int)x]);
    }
    
    private void MoveCursorsLeft()
    {
        if (_cursorX == 0)
        {
            return;
        }
        UpdateCursorsAndValues(_cursorX - 1);
    }
    
    private void MoveCursorsRight()
    {
        if ((int)_cursorX == _allIndexes.Count - 1)
        {
            return;
        }
        UpdateCursorsAndValues(_cursorX + 1);
    }

    private void UpdateCursorsAndValues(double value)
    {
        CursorX = value;
        CurrentDateTime = GetDateTimeByX(value);
        
        MoveAllCursors(value);
        ChangePageIfNeed(value);
        UpdateCurrentValues();
    }
    
    private void ApplyScales()
    {
        var (min, max) = GetRangeForCursor(_cursorX);
        StartDateTime = GetDateTimeByX(min);
        ApplyToAllPlots(plot => plot.SetRangeX(min, max));
    }
    
    private (double, double) GetRangeForCursor(double cursorX)
    {
        var min = cursorX - cursorX % _scale.Seconds;
        var max = min + _scale.Seconds;
        return (min, max);
    }

    private void ChangePageIfNeed(double cursorX)
    {
        var (min, max) = GetRangeForCursor(cursorX);
        if (Math.Abs(min - PlotBox1.XMin) > 1e-9 && Math.Abs(max - PlotBox1.XMax) > 1e-9)
        {
            ApplyToAllPlots(plot => plot.SetRangeX(min, max));
            _startDateTime = GetDateTimeByX(min);
            
            OnPropertyChanged(nameof(StartDateTime));
            UpdatePlots(_startDateTime, _scale.Seconds);
        }
    }
    
    private void MoveAllCursors(double value)
    {
        ApplyToAllPlots(plot => plot.MoveCursor(value));
    }
    
    private void ApplyToAllPlots(Action<PlotBoxViewModel> action)
    {
        foreach (var plot in _plots)
        {
            action(plot);
        }
    }
    
    private void OnPlotBoxChanged(TrendSeries trendSeries)
    {
        foreach (var plot in _plots)
        {
            if (plot.SelectedPlot == trendSeries)
            {
                plot.LoadNewPointsAndUpdate(trendSeries.Points);
            }
        }
    }
    
    private void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
