using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialMapPacket : IPacket
{
    public List<string> UnlockedNodes;

    public PacketType Type => PacketType.InitialMapEntries;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

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