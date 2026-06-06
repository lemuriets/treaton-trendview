using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace LogDecoder.Infrastructure.Logging;

public sealed class InMemoryLogSink : ILogEventSink
{
    private const int MaxEntries = 500;

    public static InMemoryLogSink Instance { get; } = new();

    private readonly ConcurrentQueue<LogEvent> _entries = new();

    public event Action<LogEvent>? LogReceived;

    public void Emit(LogEvent logEvent)
    {
        _entries.Enqueue(logEvent);

        while (_entries.Count > MaxEntries)
        {
            _entries.TryDequeue(out _);
        }

        LogReceived?.Invoke(logEvent);
    }

    public IReadOnlyList<LogEvent> GetAll()
    {
        return _entries.ToArray();
    }
}
