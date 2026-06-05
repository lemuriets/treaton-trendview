using LogDecoder.CAN.Contracts;
using LogDecoder.Parser;
using LogDecoder.Parser.Export;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>
/// Owns the <see cref="LogParser"/> lifecycle for the selected logs folder: creating the
/// session, running indexing, and surfacing index state. Parser index events are re-raised
/// as <see cref="IndexStarted"/>/<see cref="IndexFinished"/> (on the parser's thread; the
/// caller marshals to the UI thread).
/// </summary>
public sealed class IndexingService(ILogger logger, ICanPackageFactory factory)
{
    private LogParsingSession? _session;

    public event Action? IndexStarted;
    public event Action? IndexFinished;

    public LogParsingSession? Current => _session;
    public bool HasIndex => _session is { } session && session.Parser.IndexTimes.Count > 0;
    public DateTime? MinDateTime => _session?.Parser.MinDatetime;
    public DateTime? MaxDateTime => _session?.Parser.MaxDatetime;

    public LogParsingSession CreateSession(string folder)
    {
        Reset();

        var parser = new LogParser(logger, folder, factory);
        var session = new LogParsingSession(parser, new CsvExport(logger, parser));

        parser.StartIndex += OnStart;
        parser.FinishIndex += OnFinish;

        _session = session;
        return session;
    }

    public Task IndexAsync(CancellationToken cancellationToken)
    {
        if (_session is null)
        {
            throw new InvalidOperationException("No active session. Call CreateSession first.");
        }

        return _session.Parser.CreateOrLoadAllIndexesAsync(cancellationToken);
    }

    public void Reset()
    {
        if (_session is null)
        {
            return;
        }

        _session.Parser.StartIndex -= OnStart;
        _session.Parser.FinishIndex -= OnFinish;
        _session = null;
    }

    private void OnStart() => IndexStarted?.Invoke();
    private void OnFinish() => IndexFinished?.Invoke();
}
