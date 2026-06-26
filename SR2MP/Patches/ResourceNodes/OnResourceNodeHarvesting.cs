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
        if (model != null)
        {
            var packet = new ResourceNodePacket
            {
                NodeId = model.nodeId,
                State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTING,
                RequestSpawn = false
            };
            Main.SendToAllOrServer(packet);
        }
    }
}