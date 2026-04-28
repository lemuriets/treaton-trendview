using Microsoft.Extensions.Logging;

namespace LogDecoder.Infrastructure.Logging;

public sealed class LoggerProvider : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    public LoggerProvider()
    {
        _loggerFactory = LoggerConfig.CreateLoggerFactory();
    }

    public ILogger<T> CreateLogger<T>()
    {
        ThrowIfDisposed();

        return _loggerFactory.CreateLogger<T>();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _loggerFactory.Dispose();

        Serilog.Log.CloseAndFlush();

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LoggerProvider));
        }
    }
}