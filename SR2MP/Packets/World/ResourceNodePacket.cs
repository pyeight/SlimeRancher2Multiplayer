using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class ResourceNodePacket : IPacket
{
    public string NodeId = string.Empty;
    public byte State;
    public bool RequestSpawn;

    public PacketType Type => PacketType.ResourceNode;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(NodeId);
        writer.WriteByte(State);
        writer.WritePackedBool(RequestSpawn);
    }

    public void Deserialise(PacketReader reader)
    {
        NodeId = reader.ReadPooledString()!;
        State = reader.ReadByte();
        RequestSpawn = reader.ReadPackedBool();
    }
}
