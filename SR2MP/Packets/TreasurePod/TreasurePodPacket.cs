using SR2MP.Packets.Utils;

namespace SR2MP.Packets.TreasurePod;

internal sealed class TreasurePodPacket : IPacket
{
    public int ID;

    public PacketType Type => PacketType.TreasurePod;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPackedInt();
    }
}