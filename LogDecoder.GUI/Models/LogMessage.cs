using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.GUI.Models;

public class LogMessage(string text, PackageTechStatus status)
{
    public string Text { get; set; } = text;
    public PackageTechStatus Status { get; set; } = status;
}