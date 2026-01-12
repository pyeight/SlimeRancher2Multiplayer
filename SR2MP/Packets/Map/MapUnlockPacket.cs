using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Map;

public sealed class MapUnlockPacket : IPacket
{
    public PacketType Type => PacketType.MapUnlock;

    public string NodeID { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(NodeID);
    }

    public void Deserialise(PacketReader reader)
    {
        NodeID = reader.ReadString();
    }
}