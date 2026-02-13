using Il2CppMonomiPark.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public sealed class AccessDoorPacket : IPacket
{
    public string ID { get; set; }
    public AccessDoor.State State { get; set; }

    public PacketType Type => PacketType.AccessDoor;
    public PacketReliability Reliability => PacketReliability.Reliable;

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