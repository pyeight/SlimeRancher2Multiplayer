using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Landplot;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class GardenPlantPacket : PacketBase
{
    public override PacketType Type => PacketType.GardenPlant;
    
    public string ID { get; set; }
    public int ActorType { get; set; }

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteInt(ActorType);
    }

    public override void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        ActorType = reader.ReadInt();
    }
}