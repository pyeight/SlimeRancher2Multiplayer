using Il2CppMonomiPark.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialAccessDoorsPacket : IPacket
{
    public sealed class Door : INetObject
    {
        public string ID { get; set; }
        public AccessDoor.State State { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WritePackedEnum(State);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            State = reader.ReadPackedEnum<AccessDoor.State>();
        }
    }

    public List<Door> Doors { get; set; }

    public PacketType Type => PacketType.InitialAccessDoors;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) => writer.WriteList(Doors, PacketWriterDels.NetObject<Door>.Func);

    public void Deserialise(PacketReader reader) => Doors = reader.ReadList(PacketReaderDels.NetObject<Door>.Func);
}