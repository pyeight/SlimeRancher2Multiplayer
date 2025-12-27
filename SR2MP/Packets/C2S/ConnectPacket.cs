using SR2MP.Packets.Utils;

namespace SR2MP.Packets.C2S;

public sealed class ConnectPacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
    }
}