using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace LogDecoder.GUI.Avalonia.Views;

public partial class FolderSelectorView : UserControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<FolderSelectorView, string?>(nameof(Label));

    public static readonly StyledProperty<string?> PathTextProperty =
        AvaloniaProperty.Register<FolderSelectorView, string?>(nameof(PathText));

    public static readonly StyledProperty<IBrush?> PathBrushProperty =
        AvaloniaProperty.Register<FolderSelectorView, IBrush?>(nameof(PathBrush));

    public static readonly StyledProperty<bool> IsLinkProperty =
        AvaloniaProperty.Register<FolderSelectorView, bool>(nameof(IsLink));

    public static readonly StyledProperty<ICommand?> OpenCommandProperty =
        AvaloniaProperty.Register<FolderSelectorView, ICommand?>(nameof(OpenCommand));

    public FolderSelectorView()
    {
        InitializeComponent();
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? PathText
    {
        get => GetValue(PathTextProperty);
        set => SetValue(PathTextProperty, value);
    }

    public IBrush? PathBrush
    {
        get => GetValue(PathBrushProperty);
        set => SetValue(PathBrushProperty, value);
    }

    public bool IsLink
    {
        get => GetValue(IsLinkProperty);
        set => SetValue(IsLinkProperty, value);
    }

    public ICommand? OpenCommand
    {
        get => GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    private void OnPathPressed(object? sender, PointerPressedEventArgs e)
    {
        if (OpenCommand?.CanExecute(null) == true)
        {
            OpenCommand.Execute(null);
        }
    }
}
