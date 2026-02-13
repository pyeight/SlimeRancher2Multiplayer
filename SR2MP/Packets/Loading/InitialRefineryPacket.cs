using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialRefineryPacket : IPacket
{
    public Dictionary<ushort, ushort> Items;

    public PacketType Type => PacketType.InitialRefinery;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) => writer.WriteDictionary(Items, PacketWriterDels.UShort, PacketWriterDels.UShort);

    public void Deserialise(PacketReader reader) => Items = reader.ReadDictionary(PacketReaderDels.UShort, PacketReaderDels.UShort);
}