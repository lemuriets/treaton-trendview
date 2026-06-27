using LogDecoder.CAN;
using LogDecoder.CAN.Packages;

namespace LogDecoder.GUI.Avalonia.Models;

public class LogMessage(string text, PackageTechStatus status)
{
    public string Text { get; set; } = text;
    public PackageTechStatus Status { get; set; } = status;
}