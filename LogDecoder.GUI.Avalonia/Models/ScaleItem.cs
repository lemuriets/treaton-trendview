using System.ComponentModel;
using LogDecoder.GUI.Avalonia.Localization;

namespace LogDecoder.GUI.Avalonia.Models;

public class ScaleItem(int seconds) : INotifyPropertyChanged
{
    public int Seconds { get; } = seconds;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Label
    {
        get
        {
            var loc = LocalizationManager.Instance;
            if (Seconds / 60 > 0 && Seconds % 60 == 0)
            {
                return $"{Seconds / 60} {loc.Get("ScaleMinutes")}";
            }
            return $"{Seconds} {loc.Get("ScaleSeconds")}";
        }
    }

    // Re-reads Label after a language change without replacing the instance,
    // so the ComboBox keeps its selection.
    public void RefreshLabel()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
    }

    public override string ToString()
    {
        return Label;
    }
}
