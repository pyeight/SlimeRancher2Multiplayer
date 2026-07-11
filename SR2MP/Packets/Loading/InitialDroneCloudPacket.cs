using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialDroneCloudPacket : IPacket
{
    public Dictionary<int, int> Amounts = new();

    public PacketType Type => PacketType.InitialDroneCloud;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteDictionary(Amounts, PacketWriterDels.PackedInt, PacketWriterDels.PackedInt);
    }

    public void Deserialise(PacketReader reader)
    {
        Amounts = reader.ReadDictionary(PacketReaderDels.PackedInt, PacketReaderDels.PackedInt)!;
    }
}
