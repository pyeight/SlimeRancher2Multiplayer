using HarmonyLib;

namespace SR2MP.Patches.ResourceNodes;

[HarmonyPatch(typeof(ResourceNode), nameof(ResourceNode.SpawnSingleResource))]
internal static class OnResourceNodeHarvest
{
    public static bool Prefix(ResourceNode __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return true;

        if (HandlingPacket)
            return false;

        var nodeId = __instance._model?.nodeId;
        
        return nodeId == null || !ResourceNodeManager.RemotelyHarvested.Contains(nodeId);
    }
}
