namespace LogDecoder.Helpers.Csv;

using System.Text;

public sealed class CsvSession : IDisposable
{
    public string FilePath { get; }
    public StreamWriter Writer { get; }

    public CsvSession(string filePath, bool rewrite = true, Encoding? encoding = null, int bufferSize = 64 * 1024)
    {
        FilePath = filePath;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var fileMode = rewrite ? FileMode.Create : FileMode.Append;

        var stream = new FileStream(filePath, fileMode, FileAccess.Write, FileShare.Read, bufferSize);

        Writer = new StreamWriter(
            stream,
            encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            bufferSize);
    }

    public void Dispose()
    {
        Writer.Flush();
        Writer.Dispose();
    }
}

public sealed class CsvWriter
{
    private readonly StreamWriter _writer;
    private readonly char _separator;

    public CsvWriter(CsvSession session, char separator = ';')
    {
        _writer = session.Writer;
        _separator = separator;
    }

    public void AddRow(IEnumerable<string> values)
    {
        var isFirst = true;
        foreach (var value in values)
        {
            if (!isFirst)
            {
                _writer.Write(_separator);
            }
            _writer.Write(Escape(value));
            isFirst = false;
        }
        _writer.WriteLine();
    }

    private string Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        var mustQuote =
            value.Contains(_separator) ||
            value.Contains('"') ||
            value.Contains('\r') ||
            value.Contains('\n');

        if (mustQuote)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}