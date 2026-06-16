using Avalonia.Controls;
using Avalonia.Input;
using LogDecoder.GUI.Avalonia.ViewModels;
using LogDecoder.Parser;

namespace LogDecoder.GUI.Avalonia;

public partial class Trends : Window
{
    public Trends(LogParser parser)
    {
        InitializeComponent();
        DataContext = new TrendViewModel(parser);
        KeyDown += WindowKeyDown;
    }

    private void WindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not TrendViewModel vm)
        {
            return;
        }

        // Caret navigation inside the start-time TextBox must not also page the
        // plot cursor.
        if (FocusManager?.GetFocusedElement() is TextBox)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                vm.MoveCursorsLeftCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Right:
                vm.MoveCursorsRightCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    // The start-time binding commits on LostFocus (so the plots don't reload on
    // every keystroke). Enter commits by dropping focus, which raises LostFocus.
    private void StartTimeBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FocusManager?.ClearFocus();
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        KeyDown -= WindowKeyDown;
        if (DataContext is TrendViewModel vm)
        {
            vm.Dispose();
        }
        base.OnClosed(e);
    }
}
