using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.ResourceNodes;

[HarmonyPatch(typeof(ResourceNode), nameof(ResourceNode.SetStateHarvesting))]
internal static class OnResourceNodeHarvesting
{
    public static void Postfix(ResourceNode __instance)
    {
        if (HandlingPacket)
            return;

        var model = __instance._model;
        if (model == null)
            return;
        
        ResourceNodeManager.RemotelyHarvested.Remove(model.nodeId);

        Main.SendToAllOrServer(new ResourceNodePacket
        {
            NodeId = model.nodeId,
            State = (byte)ResourceNode.NodeState.HARVESTING
        });
    }
}