using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class MapUnlockPacket : PacketBase
{
    public override PacketType Type => PacketType.MapUnlock;

    public string NodeID { get; set; }

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(NodeID);
    }

    public override void Deserialise(PacketReader reader)
    {
        NodeID = reader.ReadString();
    }
}