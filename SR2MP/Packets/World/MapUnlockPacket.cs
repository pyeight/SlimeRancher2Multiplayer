using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public sealed class MapUnlockPacket : IPacket
{
    public PacketType Type => PacketType.MapUnlock;
    public PacketReliability Reliability => PacketReliability.Reliable;

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