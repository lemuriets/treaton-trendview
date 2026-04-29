using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.CanPackages;

public readonly record struct BitMessage
{
    public string MsgWhenZero { get; init; }
    public string MsgWhenOne { get; init; }
    public PackageTechStatus Status { get; init; }

    private BitMessage(string msgWhenZero, string msgWhenOne, PackageTechStatus status)
    {
        MsgWhenZero = msgWhenZero;
        MsgWhenOne = msgWhenOne;
        Status = status;
    }

    public static BitMessage Zero(string message, PackageTechStatus status = PackageTechStatus.Info)
    {
        return new BitMessage(message, string.Empty, status);
    }

    public static BitMessage One(string message, PackageTechStatus status = PackageTechStatus.Info)
    {
        return new BitMessage(string.Empty, message, status);
    }

    public static BitMessage Both(string msgWhenZero, string msgWhenOne, PackageTechStatus status = PackageTechStatus.Info)
    {
        return new BitMessage(msgWhenZero, msgWhenOne, status);
    }

    public bool TryGetMessage(bool isSet, out string message)
    {
        message = isSet ? MsgWhenOne : MsgWhenZero;

        return message.Length != 0;
    }
}