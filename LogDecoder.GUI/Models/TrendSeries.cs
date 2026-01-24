using System.Collections.ObjectModel;
using OxyPlot;

namespace LogDecoder.GUI.Models;

public class TrendSeries(string name)
{
    public string DisplayName { get; } = name;
    public ObservableCollection<DataPoint> Points { get; } = [];
    
    public void Clear()
    {
        Points.Clear();
    }

    public void Add(double x, double y)
    {
        Points.Add(new DataPoint(x, y));
    }
    
    public double GetValueAtIndexSafe(int idx)
    {
        if (idx < 0 || idx >= Points.Count)
        {
            return double.NaN;
        }
        return Points[idx].Y;
    }
}