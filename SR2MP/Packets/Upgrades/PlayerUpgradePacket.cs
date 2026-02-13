using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Upgrades;

public struct PlayerUpgradePacket : IPacket
{
    public byte UpgradeID;

    public readonly PacketType Type => PacketType.PlayerUpgrade;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;

    public readonly void Serialise(PacketWriter writer) => writer.WriteByte(UpgradeID);

    public void Deserialise(PacketReader reader) => UpgradeID = reader.ReadByte();
}