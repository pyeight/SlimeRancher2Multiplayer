using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

internal sealed class SiloSlotSelectPacket : IPacket
{
    public byte Side;
    public byte Index;
    public string ID;

    public PacketType Type => PacketType.SiloSlotSelect;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Side);
        writer.WriteByte(Index);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        Side = reader.ReadByte();
        Index = reader.ReadByte();
        ID = reader.ReadString()!;
    }
}