using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialComponentsPacket : IPacket
{
    public Dictionary<string, byte> Items;

    public PacketType Type => PacketType.InitialComponents;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Ammo;

    public void Serialise(PacketWriter writer) => writer.WriteDictionary(Items, PacketWriterDels.String, PacketWriterDels.Byte);

    public void Deserialise(PacketReader reader) => Items = reader.ReadDictionary(PacketReaderDels.String, PacketReaderDels.Byte)!;
}