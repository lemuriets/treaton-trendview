using System.Windows;
using System.Windows.Input;
using LogDecoder.GUI.ViewModels;
using LogDecoder.Parser;

namespace LogDecoder.GUI;

public partial class TrendView : Window
{
    public TrendView(LogParser parser)
    {
        InitializeComponent();
        
        DataContext = new TrendViewModel(parser);
    }
    
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
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