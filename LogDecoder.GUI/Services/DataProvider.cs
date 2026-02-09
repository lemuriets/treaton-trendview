using System.Collections.ObjectModel;
using System.Runtime.InteropServices.JavaScript;
using LogDecoder.CAN.General;
using LogDecoder.GUI.Models;
using LogDecoder.Helpers.TimeHelper;
using LogDecoder.Parser;

namespace LogDecoder.GUI.Services;

public class DataProvider(LogParser parser)
{
    public void GetDataForTimeSpan(TrendsData seriesPool, ObservableCollection<LogMessage> messages, DateTime start, int lengthSec, double startX)
    {
        var x = startX;
        var end = start.AddSeconds(lengthSec);
        var prevHrc = -1;
        foreach (var pkg in parser.GetPackages(parser.IdsAll, start, end))
        {
            if (prevHrc == -1)
            {
                prevHrc = pkg.Hrc;
            }
            Console.WriteLine(x);
            var hrcDelta = CanUtils.CalcHrcDelta(prevHrc, pkg.Hrc);
            var shift = hrcDelta / TimeHelper.MicrosecondsPerSecond;
            
            x += shift;
            prevHrc = pkg.Hrc;

            var parsedData = pkg.ParseData();
            if (parsedData is null)
            {
                continue;
            }
            
            foreach (var msg in parsedData.Value.Messages)
            {
                messages.Add(new LogMessage(msg, pkg.TechStatus));
            }

            foreach (var (name, value) in parsedData.Value.NumericData)
            {
                var series = seriesPool.GetSeriesByName(name);
                series?.Add(x, value);
            }
        }
    }

    public bool IsDateTimeExists(DateTime target)
    {
        return parser.IsDateTimeExists(target);
    }
}