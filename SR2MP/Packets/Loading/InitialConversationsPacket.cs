using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialConversationsPacket : IPacket
{
    public List<string?> ConversationNames;

    public PacketType Type => PacketType.InitialConversations;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(ConversationNames, PacketWriterDels.String);

    public void Deserialise(PacketReader reader) => ConversationNames = reader.ReadList(PacketReaderDels.String)!;
}
