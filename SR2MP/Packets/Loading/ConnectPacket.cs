using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class ConnectPacket : IPacket
{
    public string PlayerId { get; set; }
    public string Username { get; set; }

    public PacketType Type => PacketType.Connect;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteString(Username);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        Username = reader.ReadString();
    }
}