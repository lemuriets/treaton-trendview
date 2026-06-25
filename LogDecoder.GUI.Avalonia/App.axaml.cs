using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using LogDecoder.CAN.Protocol;
using LogDecoder.GUI.Avalonia.Services;
using LogDecoder.Infrastructure.Configuration;
using LogDecoder.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI.Avalonia;

public partial class App : Application
{
    private static int _criticalErrorShown;

    public override void Initialize()
    {
        try
        {
            new LanguageService(new LanguageSettingsService()).ApplySavedLanguage();
        }
        catch
        {
            // Settings IO or culture lookup must not block startup; fall through
            // to the default culture. Global handlers aren't installed yet, so a
            // throw here would crash before any error dialog could appear.
        }

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetupGlobalExceptionHandlers();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateStartupWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateStartupWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loggerProvider = new LoggerProvider();
        var logger = loggerProvider.CreateLogger<App>();

        try
        {
            var families = ProtocolLoader.ListFamilies(logger: logger);
            if (families.Count == 0)
            {
                loggerProvider.Dispose();
                return CreateFatalWindow(
                    Localizer.L("ConfigMissingTitle"),
                    Localizer.F("ConfigMissingMessage", ProtocolLoader.DefaultConfigRoot));
            }
            if (families.Count == 1)
            {
                return BuildMainWindow(families[0], loggerProvider, logger);
            }
            return CreateConfigSelectionWindow(desktop, families, loggerProvider, logger);
        }
        catch (ConfigFolderMissingException ex)
        {
            logger.LogError(ex, "Configuration folder missing; cannot start");
            loggerProvider.Dispose();
            return CreateFatalWindow(
                Localizer.L("ConfigMissingTitle"),
                Localizer.F("ConfigMissingMessage", ex.ConfigPath));
        }
        catch (ProtocolLoadException ex)
        {
            logger.LogError(ex, "Failed to load protocol configuration; cannot start");
            loggerProvider.Dispose();
            return CreateFatalWindow(Localizer.L("ConfigInvalidTitle"), Localizer.F("ConfigInvalidMessage", ex.Message));
        }
    }

    private static MainWindow BuildMainWindow(ConfigFamily family, LoggerProvider loggerProvider, ILogger logger)
    {
        var factory = ProtocolBootstrap.CreateFactory(family, logger);
        PackageDescriptionService.Initialize(family.FullPath, logger);
        SaveActiveConfig(family.FolderName);
        logger.LogInformation("Active config family: {Family}", family.FolderName);
        return new MainWindow(factory, loggerProvider);
    }

    private static void SaveActiveConfig(string folderName)
    {
        try
        {
            var config = UserConfig.LoadOrCreate();
            config.ActiveConfig = folderName;
            config.Save();
        }
        catch
        {
            // Persisting the choice is best-effort; failure must not block startup.
        }
    }

    private static Window CreateConfigSelectionWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        IReadOnlyList<ConfigFamily> families,
        LoggerProvider loggerProvider,
        ILogger logger)
    {
        var saved = UserConfig.LoadOrCreate().ActiveConfig;
        var preselect = families.ToList().FindIndex(f => f.FolderName == saved);

        var window = new ConfigSelectionWindow(
            families.Select(f => f.DisplayName).ToList(),
            preselect >= 0 ? preselect : 0);

        var proceeded = false;
        window.Accepted += index =>
        {
            Window next;
            try
            {
                next = BuildMainWindow(families[index], loggerProvider, logger);
            }
            catch (ProtocolLoadException ex)
            {
                logger.LogError(ex, "Failed to load selected configuration; cannot start");
                loggerProvider.Dispose();
                next = CreateFatalWindow(Localizer.L("ConfigInvalidTitle"), Localizer.F("ConfigInvalidMessage", ex.Message));
            }
            proceeded = true;
            desktop.MainWindow = next;
            next.Show();
            window.Close();
        };

        window.Closed += (_, _) =>
        {
            if (!proceeded)
            {
                loggerProvider.Dispose();
                Environment.Exit(0);
            }
        };

        return window;
    }

    private static Window CreateFatalWindow(string title, string message)
    {
        var okButton = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
            Foreground = Brushes.White,
            Margin = new Thickness(0, 8, 0, 12)
        };

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16)
        };

        var content = new DockPanel
        {
            Children = { okButton, new ScrollViewer { Content = textBlock } }
        };
        DockPanel.SetDock(okButton, Dock.Bottom);

        var window = new Window
        {
            Title = title,
            Width = 560,
            Height = 260,
            Content = content
        };

        okButton.Click += (_, _) => Environment.Exit(0);
        return window;
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
            Title = "Critical error",
            Width = 760,
            Height = 420,
            Content = content
        };

        okButton.Click += (_, _) => window.Close();

        return window;
    }
}