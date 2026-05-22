using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

internal sealed class PlayerJoinPacket : IPacket
{
    public string PlayerId;
    public string? PlayerName;

    public PacketType Type { get; init; }
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Important;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteStringWithoutSize(PlayerId);
        writer.WriteString(PlayerName ?? "No Name Set");
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadPooledStringOfSize(16)!;
        PlayerName = reader.ReadPooledString();
    }
}