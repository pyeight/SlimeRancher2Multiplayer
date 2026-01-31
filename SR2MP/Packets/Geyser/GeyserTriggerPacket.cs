using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Geyser;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class GeyserTriggerPacket : PacketBase
{
    // Couldnt find an ID system for these so I need to access them through GameObject.Find
    public string ObjectPath { get; set; }

    public override PacketType Type => PacketType.GeyserTrigger;

    public override void Serialise(PacketWriter writer) => writer.WriteString(ObjectPath);

    public override void Deserialise(PacketReader reader) => ObjectPath = reader.ReadString();
}