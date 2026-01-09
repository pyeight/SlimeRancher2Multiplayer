using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerJoinPacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }
    public string? PlayerName { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
        writer.WriteString(PlayerName ?? "No Name Set");
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
        PlayerName = reader.ReadString();
    }
}