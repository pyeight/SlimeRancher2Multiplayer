using Il2CppMonomiPark.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialAccessDoorsPacket : IPacket
{
    internal sealed class Door : INetObject
    {
        public string ID;
        public AccessDoor.State State;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WritePackedEnum(State);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString()!;
            State = reader.ReadPackedEnum<AccessDoor.State>();
        }
    }

    public List<Door> Doors;

    public PacketType Type => PacketType.InitialAccessDoors;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) => writer.WriteList(Doors, PacketWriterDels.NetObject<Door>.Writer);

    public void Deserialise(PacketReader reader) => Doors = reader.ReadList(PacketReaderDels.NetObject<Door>.Reader)!;
}