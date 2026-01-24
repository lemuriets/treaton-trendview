using System.ComponentModel;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

using LogDecoder.GUI.Models;

namespace LogDecoder.GUI.ViewModels;

public class PlotBoxViewModel : INotifyPropertyChanged
{
    public PlotBoxViewModel(string title, double min, double max, OxyColor color)
    {
        CreatePlot(title, min, max, color);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<TrendSeries>? PlotChanged;
    public event Action? PageChanged;

    private const double xPadding = 0.1;

    public PlotModel Model { get; private set; }
    public IEnumerable<TrendSeries> AvailablePlots { get; set; }

    private TrendSeries _selectedPlot;
    public TrendSeries SelectedPlot
    {
        get => _selectedPlot;
        set
        {
            _selectedPlot = value;
            OnPropertyChanged(nameof(SelectedPlot));

            PlotChanged?.Invoke(_selectedPlot);
        }
    }

    private Axis _xAxis;
    private Axis _yAxis;
    private LineAnnotation _cursor;

    private double _cursorX;

    private double _yValue;
    public double YValue
    {
        get => _yValue;
        set
        {
            _yValue = value;
            OnPropertyChanged(nameof(YValue));
        }
    }

    public double XMin => _xAxis.Minimum + xPadding;
    public double XMax => _xAxis.Maximum;
    
    private void CreatePlot(string title, double min, double max, OxyColor color)
    {
        Model = new PlotModel { Title = title };
        SetupPlot(min, max, color);
    }

    private void SetupPlot(double min, double max, OxyColor color)
    {
        SetupAxes(min, max);
        SetupCursor();
        LoadNewSeries(new LineSeries
        {
            Color = color,
        });
    }
        
    private void SetupAxes(double min, double max)
    {
        _xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            MinorTickSize = 0,
            // MajorStep = 1,
            // LabelFormatter = _ => ""
        };
        _yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            IsZoomEnabled = false,
            IsPanEnabled = false
        };
        SetRangeX(min, max);
        Model.Axes.Add(_xAxis);
        Model.Axes.Add(_yAxis);
    }
    
    private void SetupCursor()
    {
        _cursor = new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            Color = OxyColors.Red,
            X = 0
        };
        Model.Annotations.Add(_cursor);
    }
    
    private double GetNearestYValue(double x)
    {
        if (Model.Series.FirstOrDefault() is not LineSeries series || series.Points.Count <= 0)
        {
            return double.NaN;
        }
        var nearest = series.Points
            .OrderBy(p => Math.Abs(p.X - x))
            .First();

        return Math.Round(nearest.Y, 2);
    }

    public void SetRangeX(double min, double max)
    {
        _xAxis.Minimum = min - xPadding;
        _xAxis.Maximum = max;
        
        Model.InvalidatePlot(false);
    }

    public void LoadNewPointsAndUpdate(IEnumerable<DataPoint> points)
    {
        if (Model.Series.FirstOrDefault() is LineSeries series)
        {
           series.Points.Clear();
           series.Points.AddRange(points);
        }
        YValue = GetNearestYValue(0);
        Model.InvalidatePlot(true);
    }
    
    public void LoadNewSeries(LineSeries series)
    {
        Model.Series.Clear();
        Model.Series.Add(series);
        Model.InvalidatePlot(true);
    }
    
    public void MoveCursor(double value)
    {
        _cursorX = value;
        
        UpdateCursorAndValue(_cursorX);
    }
    
    private void UpdateCursorAndValue(double cursorX)
    {
        YValue = GetNearestYValue(cursorX);

        UpdateCursorAnnotation();
        Model.InvalidatePlot(false);
    }
    
    private void UpdateCursorAnnotation()
    {
        _cursor.X = _cursorX;
        
        Model.InvalidatePlot(false);
    }

    private void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
