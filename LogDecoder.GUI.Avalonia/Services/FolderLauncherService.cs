using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>Opens a folder in the OS file explorer.</summary>
public sealed class FolderLauncherService(ILogger logger)
{
    public void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            logger.LogWarning("Cannot open folder. Folder does not exist: {Folder}", path);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed OpenFolder({Folder})", path);
        }
    }
}
