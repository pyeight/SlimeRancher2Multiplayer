using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Upgrades;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class PlayerUpgradePacket : PacketBase
{
    public byte UpgradeID { get; set; }

    public override PacketType Type => PacketType.PlayerUpgrade;

    public override void Serialise(PacketWriter writer) => writer.WriteByte(UpgradeID);

    public override void Deserialise(PacketReader reader) => UpgradeID = reader.ReadByte();
}