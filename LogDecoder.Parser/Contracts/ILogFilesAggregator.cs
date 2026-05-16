namespace LogDecoder.Parser.Contracts;

public interface ILogFilesAggregator
{
    IReadOnlyList<string> SortedFiles { get; }
    IReadOnlyList<string> SortedFilenames { get; }

    IReadOnlyList<string> GetWrappedRange(string startFilename, string endFilename);
}
