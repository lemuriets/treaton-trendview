using System.Collections.Specialized;
using Avalonia.Controls;
using LogDecoder.GUI.Avalonia.ViewModels;

namespace LogDecoder.GUI.Avalonia;

public partial class DebugLogWindow : Window
{
    private readonly DebugLogViewModel _viewModel = new();

    public DebugLogWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.Lines.CollectionChanged += OnLinesChanged;
    }

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _viewModel.Lines.Count > 0)
        {
            LogListBox.ScrollIntoView(_viewModel.Lines[^1]);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Lines.CollectionChanged -= OnLinesChanged;
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
