using SR2MP.Packets.Utils;

namespace SR2MP.Packets.TreasurePod;

internal sealed class TreasurePodPacket : IPacket
{
    public string ID;
    public Il2Cpp.TreasurePod.State State;

    public PacketType Type => PacketType.TreasurePod;
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
        State = reader.ReadPackedEnum<Il2Cpp.TreasurePod.State>();
    }
}