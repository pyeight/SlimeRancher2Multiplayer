using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.ResourceNodes;

[HarmonyPatch(typeof(ResourceNode), nameof(ResourceNode.SetStateEmpty))]
internal static class OnResourceNodeEmpty
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
                State = (byte)ResourceNode.NodeState.HARVESTED,
                RequestSpawn = false
            };
            Main.SendToAllOrServer(packet);
        }
    }
}