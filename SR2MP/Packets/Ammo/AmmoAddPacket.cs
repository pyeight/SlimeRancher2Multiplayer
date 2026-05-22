using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Ammo;

internal sealed class AmmoAddPacket : IPacket
{
    public int Identifiable;
    public int Count;
    public bool Radiant;
    public string? ID;

    public PacketType Type => PacketType.AmmoAdd;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Ammo;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Identifiable);
        writer.WritePackedInt(Count);
        writer.WritePackedBool(Radiant);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        Identifiable = reader.ReadPackedInt();
        Count = reader.ReadPackedInt();
        Radiant = reader.ReadPackedBool();
        ID = reader.ReadPooledString();
    }
}