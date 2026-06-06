using LogDecoder.GUI.Avalonia.Localization;

namespace LogDecoder.GUI.Avalonia.Models;

public class ScaleItem(int seconds)
{
    public int Seconds { get; } = seconds;

    public override string ToString()
    {
        var loc = LocalizationManager.Instance;
        if (Seconds / 60 > 0 && Seconds % 60 == 0)
        {
            return $"{Seconds / 60} {loc.Get("ScaleMinutes")}";
        }
        return $"{Seconds} {loc.Get("ScaleSeconds")}";
    }
}