using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class ConnectPacket : IPacket
{
    public string PlayerId;
    public string Username;

    public List<ushort> ModHashes;

    public PacketType Type => PacketType.Connect;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteStringWithoutSize(PlayerId);
        writer.WriteString(Username);

        writer.WriteList(ModHashes, PacketWriterDels.UShort);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadPooledStringOfSize(16)!;
        Username = reader.ReadPooledString()!;

        ModHashes = reader.ReadList(PacketReaderDels.UShort)!;
    }
}