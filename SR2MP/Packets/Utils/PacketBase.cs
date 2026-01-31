using LiteNetLib.Utils;

namespace SR2MP.Packets.Utils;

public abstract class PacketBase : IPacket, INetSerializable
{
    public abstract PacketType Type { get; }

    public abstract void Serialise(PacketWriter writer);

    public abstract void Deserialise(PacketReader reader);

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        var packetWriter = new PacketWriter(writer);
        Serialise(packetWriter);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var packetReader = new PacketReader(reader);
        Deserialise(packetReader);
    }
}
