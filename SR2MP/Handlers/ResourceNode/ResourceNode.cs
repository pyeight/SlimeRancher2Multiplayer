using System.Net;
using Il2CppMonomiPark.SlimeRancher.World.ResourceNode;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.ResourceNode)]
internal sealed class ResourceNodeHandler : BasePacketHandler<ResourceNodePacket>
{
    protected override bool Handle(ResourceNodePacket packet, IPEndPoint? sender)
    {
        var node = FindNode(packet.NodeId);

        if (packet.RequestSpawn)
        {
            HandleSpawnRequest(packet, node);
            return false;
        }

        HandleStateUpdate(packet, node);
        return true;
    }

    private static void HandleSpawnRequest(ResourceNodePacket packet, Il2Cpp.ResourceNode? node)
    {
        if (Main.Server.IsRunning)
        {
            if (node != null)
            {
                HandlingPacket = true;
                node.SpawnSingleResource();
                HandlingPacket = false;

                Main.Server.SendToAll(new ResourceNodePacket
                {
                    NodeId = packet.NodeId,
                    State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTING,
                    RequestSpawn = false
                });
            }
            else
            {
                Main.Server.SendToAll(packet);
            }

            return;
        }

        if (node != null)
        {
            node.SpawnSingleResource();
            Main.Client.SendPacket(new ResourceNodePacket
            {
                NodeId = packet.NodeId,
                State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTING,
                RequestSpawn = false
            });
        }
    }

    private static void HandleStateUpdate(ResourceNodePacket packet, Il2Cpp.ResourceNode? node)
    {
        if (node == null)
        {
            if (Main.Server.IsRunning)
                Main.Server.SendToAll(packet);

            SrLogger.LogDebug($"ResourceNode {packet.NodeId} not found");
            return;
        }

        HandlingPacket = true;

        if (node._model != null)
        {
            node._model.nodeState = (Il2Cpp.ResourceNode.NodeState)packet.State;
            node.UpdateForState();
        }

        if (Main.Server.IsRunning)
            Main.Server.SendToAll(packet);

        HandlingPacket = false;
    }

    private static Il2Cpp.ResourceNode? FindNode(string nodeId)
    {
        foreach (var director in ResourceNodeDirector.AllResourceDirectors)
        {
            if (director == null || director.NodeSpawners == null) continue;
            foreach (var spawner in director.NodeSpawners)
            {
                if (spawner?._model?.nodeId == nodeId)
                    return spawner.AttachedNode;
            }
        }
        return null;
    }
}