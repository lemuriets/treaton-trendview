using LogDecoder.CAN.Protocol;

namespace LogDecoder.GUI.Avalonia.Services;

public sealed record ExportRequest(
    string InputFolder,
    string OutputFolder,
    IReadOnlySet<int> Ids,
    DateTime Start,
    DateTime End,
    bool IgnoreDuplicates,
    bool ExcludeEmptyTimestamps);

/// <summary>Runs a CSV export off the UI thread for a given session.</summary>
public sealed class ExportService
{
    private static readonly IReadOnlySet<PackageTechStatus> ExportStatuses = new HashSet<PackageTechStatus>
    {
        PackageTechStatus.Warning,
        PackageTechStatus.Error,
        PackageTechStatus.Critical,
        PackageTechStatus.Info,
        PackageTechStatus.Ok
    };

    public Task ExportAsync(LogParsingSession session, ExportRequest request, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            session.Export.ToCsv(
                request.InputFolder,
                request.OutputFolder,
                request.Ids,
                request.Start,
                request.End,
                ExportStatuses,
                request.IgnoreDuplicates,
                request.ExcludeEmptyTimestamps,
                cancellationToken);
        }, cancellationToken);
    }
}
