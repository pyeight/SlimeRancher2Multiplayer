using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Ammo;

internal sealed class AmmoDecrementPacket : IPacket
{
    public int SlotIndex;
    public int Count;
    public string ID;

    public PacketType Type => PacketType.AmmoDecrement;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(SlotIndex);
        writer.WritePackedInt(Count);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        SlotIndex = reader.ReadPackedInt();
        Count = reader.ReadPackedInt();
        ID = reader.ReadString();
    }
}