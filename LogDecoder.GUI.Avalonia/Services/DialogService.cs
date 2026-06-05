using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>
/// Shows standard message dialogs. The owning <see cref="Window"/> is supplied lazily so the
/// service does not depend on a concrete window.
/// </summary>
public sealed class DialogService(Func<Window?> ownerProvider)
{
    public async Task ShowMessageAsync(string title, string text)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.Ok);

        var owner = ownerProvider();
        if (owner is null)
        {
            await box.ShowWindowAsync();
            return;
        }

        await box.ShowWindowDialogAsync(owner);
    }
}
