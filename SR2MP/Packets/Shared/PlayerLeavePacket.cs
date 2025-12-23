using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

// We should make the PlayerId come from the endpoint of the sender, if possible
public struct PlayerLeavePacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }

    public readonly void Serialise(PacketWriter writer)
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