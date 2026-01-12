namespace SR2MP.Packets.Utils;

public interface IPacket : INetObject
{
    PacketType Type { get; }
}