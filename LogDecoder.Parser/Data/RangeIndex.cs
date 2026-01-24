namespace LogDecoder.Parser.Data;

public class Range<T> where T :  IComparable<T>
{
    public Range(T from, T to, Func<T, T, int> diffFunc)
    {
        if (from.CompareTo(to) > 0)
        {
            throw new ArgumentOutOfRangeException("<from> cannot be greater than <to>.");
        }
        From = from;
        To = to;
        _diffFunc = diffFunc;
    }
    
    public readonly T From;
    public readonly T To;
    private readonly Func<T, T, int> _diffFunc;
    
    public bool Contains(T target)
    {
        return From.CompareTo(target) <= 0  && To.CompareTo(target) >= 0;
    }

    public int IndexOf(T target)
    {
        if (!Contains(target))
        {
            return -1;
        }
        return _diffFunc(target, From);
    }
}

public class DateTimeRangeIndex
{
    private List<Range<DateTime>> _index;

    public void Add(DateTime from, DateTime to)
    {
        _index.Add(new Range<DateTime>(from, to, (a, b) => (int)(a - b).TotalSeconds));
    }

    public Range<DateTime>? GetNearestRange(DateTime target)
    {
        foreach (var i in _index)
        {
            if (i.From > target)
            {
                return i;
            }
        }
        return null;
    }
}
