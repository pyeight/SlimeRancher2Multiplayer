using SR2MP.Packets.Utils;

namespace SR2MP.Packets.TreasurePod;

public sealed class TreasurePodPacket : IPacket
{
    public int ID;

    public PacketType Type => PacketType.TreasurePod;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPackedInt();
    }
}