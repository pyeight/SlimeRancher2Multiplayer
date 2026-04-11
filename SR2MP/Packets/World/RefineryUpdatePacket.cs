using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class RefineryUpdatePacket : IPacket
{
    public ushort ItemCount;
    public ushort ItemID;

    public PacketType Type => PacketType.RefineryUpdate;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Ammo;

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