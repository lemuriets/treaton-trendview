using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LogDecoder.GUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        ShowCriticalErrorAndExit(e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
                        ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown critical error");

        ShowCriticalErrorAndExit(exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();

        ShowCriticalErrorAndExit(e.Exception);
    }

    private static void ShowCriticalErrorAndExit(Exception exception)
    {
        try
        {
            MessageBox.Show(
                exception.ToString(),
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Environment.Exit(1);
        }
    }
}