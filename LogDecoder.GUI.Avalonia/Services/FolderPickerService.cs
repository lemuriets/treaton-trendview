using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>
/// Shows the OS folder picker. The owning <see cref="TopLevel"/> is supplied lazily so the
/// service does not depend on a concrete window.
/// </summary>
public sealed class FolderPickerService(Func<TopLevel?> topLevelProvider)
{
    public async Task<string?> PickFolderAsync(string title)
    {
        var topLevel = topLevelProvider();
        if (topLevel is null)
        {
            return null;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = title
        });

        if (folders.Count == 0)
        {
            return null;
        }

        return folders[0].TryGetLocalPath();
    }
}
