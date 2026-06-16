using LogDecoder.Parser;
using LogDecoder.Parser.Data;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>
/// Navigates the ordered index points (timestamps) produced by the parser.
/// IndexTimes is flattened across sessions in chronological order, so two
/// adjacent points separated by &gt;= <see cref="IndexParser.MinIntervalBetweenSessions"/>
/// belong to different ring-buffer "windows". MoveNext/MovePrev refuse to
/// cross such a boundary so the cursor never silently jumps between sessions.
/// The very first/last points clamp.
/// </summary>
public class IndexNavigator(LogParser parser)
{
    private readonly IReadOnlyList<DateTime> _times = parser.IndexTimes;
    private int _cursor;

    public int Count => _times.Count;
    public bool HasPoints => _times.Count > 0;
    public int CursorIndex => _cursor;

    public DateTime Current => _times[_cursor];

    public bool IsAtStart => _cursor <= 0;
    public bool IsAtEnd => _cursor >= _times.Count - 1;

    public bool MoveNext()
    {
        if (_cursor >= _times.Count - 1)
        {
            return false;
        }
        if (IsSessionBoundary(_times[_cursor], _times[_cursor + 1]))
        {
            return false;
        }
        _cursor++;
        return true;
    }

    public bool MovePrev()
    {
        if (_cursor <= 0)
        {
            return false;
        }
        if (IsSessionBoundary(_times[_cursor - 1], _times[_cursor]))
        {
            return false;
        }
        _cursor--;
        return true;
    }

    private static bool IsSessionBoundary(DateTime a, DateTime b)
    {
        return (b - a).Duration() >= IndexParser.MinIntervalBetweenSessions;
    }

    /// <summary>
    /// Positions the cursor at the last index point with time &lt;= target (the
    /// floor), falling back to the first point when target precedes all points.
    /// Linear scan, per project convention (binary search lives only in
    /// LogSessionsSorted).
    /// </summary>
    public bool SeekFloor(DateTime target)
    {
        if (_times.Count == 0)
        {
            return false;
        }

        var found = 0;
        for (var i = 0; i < _times.Count; i++)
        {
            if (_times[i] > target)
            {
                break;
            }
            found = i;
        }
        _cursor = found;
        return true;
    }
}
