using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class ConversationPlayedPacket : IPacket
{
    public string ConversationName;

    public PacketType Type => PacketType.ConversationPlayed;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteString(ConversationName);

    public void Deserialise(PacketReader reader) => ConversationName = reader.ReadPooledString()!;
}
