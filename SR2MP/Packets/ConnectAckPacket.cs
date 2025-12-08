using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public sealed class ConnectAckPacket : IPacket
{
    public byte Type { get; set; }
    public string AssignedPlayerId { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(AssignedPlayerId);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        AssignedPlayerId = reader.ReadString();
    }
}