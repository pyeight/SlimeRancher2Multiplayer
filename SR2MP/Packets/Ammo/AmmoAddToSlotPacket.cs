using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Ammo;

internal sealed class AmmoAddToSlotPacket : IPacket
{
    public int Identifiable;
    public int SlotIndex;
    public int Count;
    public bool Radiant;
    public string? ID;

    public PacketType Type => PacketType.AmmoAddToSlot;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Ammo;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Identifiable);
        writer.WritePackedInt(SlotIndex);
        writer.WritePackedInt(Count);
        writer.WritePackedBool(Radiant);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        Identifiable = reader.ReadPackedInt();
        SlotIndex = reader.ReadPackedInt();
        Count = reader.ReadPackedInt();
        Radiant = reader.ReadPackedBool();
        ID = reader.ReadPooledString();
    }
}