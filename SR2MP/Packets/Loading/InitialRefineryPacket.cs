using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialRefineryPacket : IPacket
{
    public Dictionary<int, ushort> Items;

    public PacketType Type => PacketType.InitialRefinery;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Ammo;

    public void Serialise(PacketWriter writer) => writer.WriteDictionary(Items, PacketWriterDels.PackedInt, PacketWriterDels.UShort);

    public void Deserialise(PacketReader reader) => Items = reader.ReadDictionary(PacketReaderDels.PackedInt, PacketReaderDels.UShort)!;
}