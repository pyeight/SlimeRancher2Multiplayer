using Il2CppMonomiPark.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class InitialAccessDoorsPacket : PacketBase
{
    public sealed class Door : INetObject
    {
        public string ID { get; set; }
        public AccessDoor.State State { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteEnum(State);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            State = reader.ReadEnum<AccessDoor.State>();
        }
    }

    public List<Door> Doors { get; set; }

    public override PacketType Type => PacketType.InitialAccessDoors;

    public override void Serialise(PacketWriter writer) => writer.WriteList(Doors, PacketWriterDels.NetObject<Door>.Func);

    public override void Deserialise(PacketReader reader) => Doors = reader.ReadList(PacketReaderDels.NetObject<Door>.Func);
}
