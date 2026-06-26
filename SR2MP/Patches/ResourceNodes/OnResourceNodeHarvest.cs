using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.ResourceNodes;

[HarmonyPatch(typeof(ResourceNode), nameof(ResourceNode.SpawnSingleResource))]
internal static class OnResourceNodeHarvest
{
    public static bool Prefix(ResourceNode __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return true;
        
        if (HandlingPacket)
            return true;

        var model = __instance._model;
        if (model != null)
        {
            var packet = new ResourceNodePacket
            {
                NodeId = model.nodeId,
                State = (byte)ResourceNode.NodeState.HARVESTING,
                RequestSpawn = true
            };
            Main.SendToAllOrServer(packet);
        }

        return false;
    }
}
