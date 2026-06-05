using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LogDecoder.Parser;
using LogDecoder.GUI.Avalonia.Commands;
using LogDecoder.GUI.Avalonia.Models;
using LogDecoder.GUI.Avalonia.Services;
using OxyPlot;

namespace LogDecoder.GUI.Avalonia.ViewModels;

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
        _navigator = new IndexNavigator(parser);

        _scale = Scales[0];

        PlotBox1 = new PlotBoxViewModel("", 0, _scale.Seconds, OxyColors.Lime);
        PlotBox2 = new PlotBoxViewModel("", 0, _scale.Seconds, OxyColors.Red);
        PlotBox3 = new PlotBoxViewModel("", 0, _scale.Seconds, OxyColors.Blue);

        _plots = [ PlotBox1, PlotBox2, PlotBox3 ];

        SetupPlots();

        if (_navigator.HasPoints)
        {
            _windowStart = _navigator.Current;
            CurrentDateTime = _navigator.Current;
        }

        ApplyScales();

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

    public double ValBattery => AllSeries.Battery.GetValueNearestX(_cursorX);
    public double ValPIP => AllSeries.PIP.GetValueNearestX(_cursorX);
    public double ValPEEP => AllSeries.PEEP.GetValueNearestX(_cursorX);
    public double ValMVTotal => AllSeries.MVTotal.GetValueNearestX(_cursorX);
    public double ValMVMachine => AllSeries.MVMachine.GetValueNearestX(_cursorX);
    public double ValVt => AllSeries.Vt.GetValueNearestX(_cursorX);
    public double ValRB1 => AllSeries.RB1.GetValueNearestX(_cursorX);
    public double ValRB2 => AllSeries.RB2.GetValueNearestX(_cursorX);
    // public double ValCst => AllSeries.Cst.GetValueNearestX(_cursorX);
    // public double ValRst => AllSeries.Rst.GetValueNearestX(_cursorX);
    public double ValLeak => AllSeries.Leak.GetValueNearestX(_cursorX);

    // _windowStart is the time mapped to plot x = 0; the visible window is
    // [_windowStart, _windowStart + Scale.Seconds].
    private DateTime _windowStart;
    public DateTime StartDateTime
    {
        get => _windowStart;
        set
        {
            if (!_dataProvider.IsDateTimeExists(value))
            {
                TxtError = global::LogDecoder.GUI.Avalonia.Localizer.L("TrendsDateNotFound");
                return;
            }
            TxtError = "";

            _navigator.SeekFloor(value);
            _windowStart = _navigator.Current;
            CurrentDateTime = _navigator.Current;

            OnPropertyChanged(nameof(StartDateTime));

            SetCursorX(0);
            ReloadWindow();
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
    private readonly IndexNavigator _navigator;

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
        // OnPropertyChanged(nameof(ValCst));
        // OnPropertyChanged(nameof(ValRst));
        OnPropertyChanged(nameof(ValLeak));
    }

    private void UpdatePlots(DateTime start, int lenghtSec)
    {
        AllSeries.ClearAll();
        Messages.Clear();

        _dataProvider.GetDataForTimeSpan(AllSeries, Messages, start, lenghtSec, 0);

        ApplyToAllPlots(plot => plot.LoadNewPointsAndUpdate(plot.SelectedPlot.Points));
    }

    private DateTime GetDateTimeByX(double x)
    {
        return _windowStart.AddSeconds(x);
        // return DateTime.Parse(_allIndexes[(int)x]);
    }

    private void MoveCursorsLeft()
    {
        if (!_navigator.MovePrev())
        {
            return;
        }
        SyncCursorToNavigator(pageBackward: true);
    }

    private void MoveCursorsRight()
    {
        if (!_navigator.MoveNext())
        {
            return;
        }
        SyncCursorToNavigator(pageBackward: false);
    }

    private void SyncCursorToNavigator(bool pageBackward)
    {
        var current = _navigator.Current;
        RepageIfNeeded(current, pageBackward);

        CurrentDateTime = current;

        var x = (current - _windowStart).TotalSeconds;
        SetCursorX(x);
    }

    // Re-anchors the window when the cursor leaves [_windowStart, +Scale].
    // Forward: cursor lands at the left edge. Backward: at the right edge, so
    // there is room to keep stepping in the same direction before re-paging.
    private void RepageIfNeeded(DateTime current, bool pageBackward)
    {
        var windowEnd = _windowStart.AddSeconds(_scale.Seconds);
        if (current >= _windowStart && current < windowEnd)
        {
            return;
        }

        _windowStart = pageBackward
            ? current.AddSeconds(-_scale.Seconds)
            : current;

        OnPropertyChanged(nameof(StartDateTime));
        ReloadWindow();
    }

    private void SetCursorX(double x)
    {
        CursorX = x;
        MoveAllCursors(x);
    }

    private void ReloadWindow()
    {
        ApplyToAllPlots(plot => plot.SetRangeX(0, _scale.Seconds));
        UpdatePlots(_windowStart, _scale.Seconds);
    }

    private void ApplyScales()
    {
        if (!_navigator.HasPoints)
        {
            ApplyToAllPlots(plot => plot.SetRangeX(0, _scale.Seconds));
            return;
        }

        var current = _navigator.Current;
        var windowEnd = _windowStart.AddSeconds(_scale.Seconds);
        if (current < _windowStart || current >= windowEnd)
        {
            _windowStart = current;
            OnPropertyChanged(nameof(StartDateTime));
        }

        ReloadWindow();
        SetCursorX((current - _windowStart).TotalSeconds);
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
