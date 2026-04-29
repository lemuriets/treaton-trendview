using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.CanPackages;

public readonly record struct MessageDefinition(
    string Message,
    PackageTechStatus Status = PackageTechStatus.Info);