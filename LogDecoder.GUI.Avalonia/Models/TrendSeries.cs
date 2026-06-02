using System.Collections.ObjectModel;
using OxyPlot;

namespace LogDecoder.GUI.Avalonia.Models;

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

    public double GetValueNearestX(double x)
    {
        if (Points.Count == 0)
        {
            return double.NaN;
        }

        var nearest = Points[0];
        var bestDist = Math.Abs(Points[0].X - x);
        for (var i = 1; i < Points.Count; i++)
        {
            var dist = Math.Abs(Points[i].X - x);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = Points[i];
            }
        }
        return nearest.Y;
    }
}