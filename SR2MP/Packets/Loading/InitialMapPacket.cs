using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialMapPacket : IPacket
{
    public PacketType Type => PacketType.InitialMapEntries;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public List<string> UnlockedNodes { get; set; }

    // todo: Add navigation marker data later.

    public void Serialise(PacketWriter writer)
    {
        writer.WriteList(UnlockedNodes, PacketWriterDels.String);
    }

    public void Deserialise(PacketReader reader)
    {
        UnlockedNodes = reader.ReadList(PacketReaderDels.String);
    }
}