using LogDecoder.Parser;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>
/// Navigates the ordered index points (timestamps) produced by the parser.
/// Movement is by index point: this naturally skips gaps and crosses session
/// boundaries, because the ordinal after a session's last point is the next
/// session's first point. The very first/last points clamp.
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
        _cursor++;
        return true;
    }

    public bool MovePrev()
    {
        if (_cursor <= 0)
        {
            return false;
        }
        _cursor--;
        return true;
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
