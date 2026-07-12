using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class PrismaBarrierPacket : IPacket
{
    public string ID;
    public double ActivationTime;

    public PacketType Type => PacketType.PrismaBarrier;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteDouble(ActivationTime);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPooledString()!;
        ActivationTime = reader.ReadDouble();
    }
}
