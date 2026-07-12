using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class InitialResourceNodesPacket : IPacket
{
    public List<ResourceNodePlacement> Nodes = new();

    public PacketType Type => PacketType.InitialResourceNodes;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteInt(Nodes.Count);
        foreach (var node in Nodes)
            node.Serialise(writer);
    }

    public void Deserialise(PacketReader reader)
    {
        var count = reader.ReadInt();
        for (var i = 0; i < count; i++)
        {
            var node = new ResourceNodePlacement();
            node.Deserialise(reader);
            Nodes.Add(node);
        }
    }
}