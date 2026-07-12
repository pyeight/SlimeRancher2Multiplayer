using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class RefineryUpdatePacket : IPacket
{
    public ushort ItemCount;
    public int ItemID;

    public PacketType Type => PacketType.RefineryUpdate;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;
    public NetworkChannel Channel => NetworkChannel.Ammo;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteUShort(ItemCount);
        writer.WritePackedInt(ItemID);
    }

    public void Deserialise(PacketReader reader)
    {
        ItemCount = reader.ReadUShort();
        ItemID = reader.ReadPackedInt();
    }
}