using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using LogDecoder.GUI.Avalonia.Services;

namespace LogDecoder.GUI.Avalonia;

public partial class App : Application
{
    private static int _criticalErrorShown;

    public override void Initialize()
    {
        new LanguageService(new LanguageSettingsService()).ApplySavedLanguage();

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetupGlobalExceptionHandlers();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void SetupGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception
                ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown critical error");

            ShowCriticalErrorAndExit(exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            e.SetObserved();
            ShowCriticalErrorAndExit(e.Exception);
        };
    }

    private static void ShowCriticalErrorAndExit(Exception exception)
    {
        if (Interlocked.Exchange(ref _criticalErrorShown, 1) == 1)
        {
            Environment.Exit(1);
            return;
        }

        try
        {
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var window = CreateCriticalErrorWindow(exception);

                    var owner = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                    if (owner is not null)
                    {
                        await window.ShowDialog(owner);
                    }
                    else
                    {
                        window.Show();
                    }
                }
                catch
                {
                    // В обработчике критических ошибок нельзя выбрасывать новое исключение,
                    // иначе можно получить рекурсию UnhandledException -> ShowCriticalErrorAndExit.
                }
                finally
                {
                    Environment.Exit(1);
                }
            });
        }
        catch
        {
            Environment.Exit(1);
        }
    }

    private static Window CreateCriticalErrorWindow(Exception exception)
    {
        var okButton = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 12)
        };

        var textBlock = new TextBlock
        {
            Text = exception.ToString(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16)
        };

        var scrollViewer = new ScrollViewer
        {
            Content = textBlock
        };

        var content = new DockPanel
        {
            Children =
            {
                okButton,
                scrollViewer
            }
        };

        DockPanel.SetDock(okButton, Dock.Bottom);

        var window = new Window
        {
            Title = Localizer.L("CriticalErrorTitle"),
            Width = 760,
            Height = 420,
            Content = content
        };

        okButton.Click += (_, _) => window.Close();

        return window;
    }
}