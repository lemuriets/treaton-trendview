namespace LogDecoder.GUI.Models;

public class ScaleItem(int seconds)
{
    public int Seconds { get; } = seconds;

    public override string ToString()
    {
        if (Seconds / 60 > 0 &&  Seconds % 60 == 0)
        {
            return  $"{Seconds / 60} мин";
        }
        return $"{Seconds} сек";
    }
}