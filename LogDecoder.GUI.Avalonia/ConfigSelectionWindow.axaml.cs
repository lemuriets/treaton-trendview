using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace LogDecoder.GUI.Avalonia;

public partial class ConfigSelectionWindow : Window
{
    public event Action<int>? Accepted;

    public ConfigSelectionWindow()
    {
        InitializeComponent();
    }

    public ConfigSelectionWindow(IReadOnlyList<string> displayNames, int selectedIndex)
        : this()
    {
        ConfigList.ItemsSource = displayNames;
        ConfigList.SelectedIndex = selectedIndex;

        OkButton.Click += OnOkClick;
        ConfigList.DoubleTapped += OnListDoubleTapped;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Dispatcher.UIThread.Post(() =>
        {
            var index = ConfigList.SelectedIndex >= 0 ? ConfigList.SelectedIndex : 0;
            if (ConfigList.ContainerFromIndex(index) is Control container)
            {
                container.Focus(NavigationMethod.Directional);
            }
            else
            {
                ConfigList.Focus();
            }
        });
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => RaiseAccepted();

    private void OnListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if ((e.Source as Control)?.FindAncestorOfType<ListBoxItem>() is not null
            && ConfigList.SelectedIndex >= 0)
        {
            RaiseAccepted();
        }
    }

    private void RaiseAccepted()
    {
        var index = ConfigList.SelectedIndex >= 0 ? ConfigList.SelectedIndex : 0;
        Accepted?.Invoke(index);
    }
}
