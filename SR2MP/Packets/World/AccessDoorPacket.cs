using Il2CppMonomiPark.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class AccessDoorPacket : PacketBase
{
    public override PacketType Type => PacketType.AccessDoor;

    public string ID { get; set; }
    
    public AccessDoor.State State { get; set; }

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteEnum(State);
    }

    public override void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        State = reader.ReadEnum<AccessDoor.State>();
    }
}