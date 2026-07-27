using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class PlortDepositorPacket : IPacket
{
    public string ID;
    public int AmountDeposited;

    public PacketType Type => PacketType.PlortDepositor;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteInt(AmountDeposited);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPooledString()!;
        AmountDeposited = reader.ReadInt();
    }
}
