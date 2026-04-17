using Il2CppMonomiPark.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class AccessDoorPacket : IPacket
{
    public string ID;
    public AccessDoor.State State;

    public PacketType Type => PacketType.AccessDoor;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WritePackedEnum(State);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPooledString()!;
        State = reader.ReadPackedEnum<AccessDoor.State>();
    }
}