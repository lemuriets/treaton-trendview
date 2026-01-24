using System.Collections.ObjectModel;
using OxyPlot;

namespace LogDecoder.GUI.Models;

public class TrendsData
{
    public TrendSeries Battery { get; } = new("Battery");
    public TrendSeries Flow { get; } = new("Flow");
    public TrendSeries Paw { get; } = new("Paw");
    public TrendSeries Vol { get; } = new("Vol");
    public TrendSeries PIP { get; } = new("PIP");
    public TrendSeries PEEP { get; } = new("PEEP");
    public TrendSeries MVTotal { get; } = new("MVTotal");
    public TrendSeries MVMachine { get; } = new("MVMachine");
    public TrendSeries Vt { get; } = new("Vt");
    public TrendSeries RB1 { get; } = new("RB1");
    public TrendSeries RB2 { get; } = new("RB2");
    public TrendSeries Cst { get; } = new("Cst");
    public TrendSeries Rst { get; } = new("Rst");
    public TrendSeries Leak { get; } = new("Leak");

    public IEnumerable<TrendSeries> AllSeries => [Battery, Flow, Paw, Vol, PIP, PEEP, MVTotal, MVMachine, Vt, RB1, RB2, Cst, Rst, Leak];

    public TrendSeries? GetSeriesByName(string name)
    {
        return AllSeries.FirstOrDefault(s => s.DisplayName.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }

    public void ClearAll()
    {
        foreach (var series in AllSeries)
        {
            series.Clear();
        }
    }
}