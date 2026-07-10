using HarmonyLib;
using SR2MP.Packets.Actor;

namespace SR2MP.Patches.Gadget;

[HarmonyPatch(typeof(Destroyer), nameof(Destroyer.DestroyGadget))]
internal static class OnGadgetDestroy
{
    public static void Prefix(GameObject gadgetObj, string source)
    {
        if (SystemContext.Instance.SceneLoader.IsSceneLoadInProgress) return;

        if ((!Main.Server.IsRunning && !Main.Client.IsConnected) || HandlingPacket || !gadgetObj)
            return;
        
        if (source is "SR2MP.ActorDestroyHandler" or "SR2MP.InitialActors" or "SR2MP.RemoveExistingGadgetModel")
            return;

        var gadget = gadgetObj.GetComponent<Il2CppMonomiPark.SlimeRancher.World.Gadget>();
        if (!gadget)
            return;

        try
        {
            var packet = new ActorDestroyPacket { ActorId = gadget.GetActorId() };
            Main.SendToAllOrServer(packet);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send GadgetDestroy packet: {ex.Message}");
        }
    }
}