using LogDecoder.CAN.General;

namespace LogDecoder.Parser.Data;

internal sealed class HrcTimeEstimator
{
    private DateTime _anchorTime;
    private int _prevHrc;
    private long _microsSinceAnchor;
    private bool _anchored;

    public bool IsAnchored => _anchored;

    public void Anchor(DateTime time, int hrc)
    {
        _anchorTime = time;
        _prevHrc = hrc;
        _microsSinceAnchor = 0;
        _anchored = true;
    }

    public DateTime Observe(int hrc)
    {
        if (!_anchored)
        {
            throw new InvalidOperationException("HrcTimeEstimator.Observe called before Anchor");
        }
        if (!CanUtils.TryComputeHrcDelta(_prevHrc, hrc, out var delta))
        {
            throw new InvalidOperationException(
                $"HRC delta is unreliable (prev=0x{_prevHrc:X6}, hrc=0x{hrc:X6}) — device counter looks discontinuous");
        }
        _microsSinceAnchor += delta;
        _prevHrc = hrc;
        return _anchorTime.AddTicks(_microsSinceAnchor * TimeSpan.TicksPerMicrosecond);
    }
}
