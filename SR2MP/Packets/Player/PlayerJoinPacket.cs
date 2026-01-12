using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerJoinPacket : IPacket
{
    public string PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public PacketType Type { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteString(PlayerName ?? "No Name Set");
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        PlayerName = reader.ReadString();
    }
}