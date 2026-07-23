using SR2MP.Packets.Utils;

namespace SR2MP.Packets.GordoSlime;

internal sealed class GordoSlimeSeenPacket : IPacket
{
    public string ID;

    public PacketType Type => PacketType.GordoSlimeSeen;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteString(ID);

    public void Deserialise(PacketReader reader) => ID = reader.ReadPooledString()!;
}
