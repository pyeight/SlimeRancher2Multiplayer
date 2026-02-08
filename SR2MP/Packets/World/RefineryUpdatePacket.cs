using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public sealed class RefineryUpdatePacket : IPacket
{
    public ushort ItemCount { get; set; }
    public ushort ItemID { get; set; }
    
    public PacketType Type => PacketType.RefineryUpdate;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteUShort(ItemCount);
        writer.WriteUShort(ItemID);
    }

    public void Deserialise(PacketReader reader)
    {
        ItemCount = reader.ReadUShort();
        ItemID = reader.ReadUShort();
    }
}