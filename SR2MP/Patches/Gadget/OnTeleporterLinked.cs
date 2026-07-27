using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using SR2MP.Packets.Gadget;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Gadget;

[HarmonyPatch(typeof(TeleporterModel), nameof(TeleporterModel.AddDestination))]
internal static class OnTeleporterLinked
{
    public static void Postfix(TeleporterModel __instance, TeleporterNodeModel nodeModel, SceneGroup sceneGroup)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (HandlingPacket) return;

        try
        {
            var sourceNode = __instance.source?.TeleporterNodeModel;
            if (sourceNode == null || nodeModel == null) return;

            SrLogger.LogDebug($"OnTeleporterLinked: broadcasting {sourceNode.NodeId} -> {nodeModel.NodeId}.");

            Main.SendToAllOrServer(new TeleporterLinkPacket
            {
                SourceNodeId = sourceNode.NodeId,
                DestinationNodeId = nodeModel.NodeId,
                DestinationSceneGroup = (byte)NetworkSceneManager.GetPersistentID(sceneGroup)
            });
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"OnTeleporterLinked: failed to broadcast link: {ex.Message}");
        }
    }
}
