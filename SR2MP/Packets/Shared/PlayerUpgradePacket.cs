using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct PlayerUpgradePacket : IPacket
{
    public byte Type { get; set; }
    public byte UpgradeID { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteByte(UpgradeID);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        UpgradeID = reader.ReadByte();
    }
}