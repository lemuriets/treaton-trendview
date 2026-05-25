using LogDecoder.Helpers.TimeHelper;

namespace LogDecoder.CAN.General;

public static class CanUtils
{
    public static bool TryComputeHrcDelta(int prevHrc, int hrc, out int delta)
    {
        var forward = hrc - prevHrc;
        // Forward jump > 1s combined with a small new hrc looks like the device
        // counter restarted — caller cannot trust a wrap-adjusted delta here.
        if (forward > TimeHelper.MicrosecondsPerSecond && hrc < TimeHelper.MicrosecondsPerSecond)
        {
            delta = 0;
            return false;
        }

        delta = hrc >= prevHrc
            ? hrc - prevHrc
            : (CanConfig.MaxHrcSize - prevHrc) + hrc;
        return true;
    }
}
