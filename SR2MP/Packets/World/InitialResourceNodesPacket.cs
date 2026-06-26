using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class InitialResourceNodesPacket : IPacket
{
    public List<Node> Nodes = new();

    public PacketType Type => PacketType.InitialResourceNodes;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteInt(Nodes.Count);
        foreach (var node in Nodes)
        {
            writer.WriteString(node.ID);
            writer.WriteByte(node.State);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        var count = reader.ReadInt();
        for (var i = 0; i < count; i++)
        {
            Nodes.Add(new Node
            {
                ID = reader.ReadPooledString()!,
                State = reader.ReadByte()
            });
        }
    }

    internal sealed class Node
    {
        public string ID = string.Empty;
        public byte State;
    }
}