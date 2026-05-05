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
}
