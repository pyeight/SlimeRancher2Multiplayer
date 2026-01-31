using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gordo;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class GordoFeedPacket : PacketBase
{
    public string ID { get; set; }
    public int NewFoodCount { get; set; }

    // Needed for unregistered gordos.
    public int RequiredFoodCount { get; set; }
    public int GordoType { get; set; }

    public override PacketType Type => PacketType.GordoFeed;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteInt(NewFoodCount);
        writer.WriteInt(RequiredFoodCount);
        writer.WriteInt(GordoType);
    }

    public override void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        NewFoodCount = reader.ReadInt();
        RequiredFoodCount = reader.ReadInt();
        GordoType = reader.ReadInt();
    }
}