namespace SR2MP.Packets.Utils;

internal interface IPacket : IReliabilityNetObject
{
    PacketType Type { get; }
}