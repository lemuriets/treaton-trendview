using LogDecoder.Helpers.TimeHelper;

namespace LogDecoder.CAN.General;

public static class CanUtils
{
    public static int CalcHrcDelta(int prevHrc, int hrc)
    {
        var delta = hrc - prevHrc;
        // не придумал нормальную реализацию
        // если дельта слишком большая - считаем, что началась новая сессия работы аппарата, значит hrc - маленькое
        if (delta > TimeHelper.MicrosecondsPerSecond && hrc < TimeHelper.MicrosecondsPerSecond)
        {
            Console.WriteLine(delta);
            return hrc;
        }
        return hrc >= prevHrc
            ? hrc - prevHrc
            : (CanConfig.MaxHrcSize - prevHrc) + hrc;
    }
}