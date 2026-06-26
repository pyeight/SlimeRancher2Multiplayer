using System.Net;
using Il2CppMonomiPark.SlimeRancher.World.ResourceNode;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.InitialResourceNodes)]
internal sealed class InitialResourceNodesHandler : BasePacketHandler<InitialResourceNodesPacket>
{
    protected override bool Handle(InitialResourceNodesPacket packet, IPEndPoint? _)
    {
        foreach (var node in packet.Nodes)
        {
            var attached = FindNode(node.ID);
            if (attached?._model == null)
                continue;

            HandlingPacket = true;
            attached._model.nodeState = (Il2Cpp.ResourceNode.NodeState)node.State;
            attached.UpdateForState();
            HandlingPacket = false;
        }

        return true;
    }

    private static Il2Cpp.ResourceNode? FindNode(string nodeId)
    {
        foreach (var director in ResourceNodeDirector.AllResourceDirectors)
        {
            if (director == null || director.NodeSpawners == null) continue;
            foreach (var spawner in director.NodeSpawners)
            {
                if (spawner != null && spawner._model != null && spawner._model.nodeId == nodeId)
                    return spawner.AttachedNode;
            }
        }
        return null;
    }
}