using LogDecoder.CAN.Contracts;

namespace LogDecoder.Parser.Contracts;

public interface ILogParser
{
    event Action? StartIndex;
    event Action? FinishIndex;

    IReadOnlySet<int> RegisteredIds { get; }
    int SynchroId { get; }
    IReadOnlyList<DateTime> IndexTimes { get; }
    DateTime? MinDatetime { get; }
    DateTime? MaxDatetime { get; }

    bool IsDateTimeExists(DateTime target);

    void CreateOrLoadAllIndexes();
    Task CreateOrLoadAllIndexesAsync(CancellationToken cancellationToken = default);

    IEnumerable<ICanPackageParsed> GetPackages(IReadOnlySet<int> filterIds, DateTime start, DateTime end);
}
