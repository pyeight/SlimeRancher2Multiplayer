using HarmonyLib;
using SR2MP.Packets.Actor;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(Destroyer), nameof(Destroyer.DestroyActor), typeof(GameObject), typeof(string), typeof(bool))]
internal static class OnActorDestroy
{
    public static bool Prefix(GameObject actorObj, string source)
    {
        if (SystemContext.Instance.SceneLoader.IsSceneLoadInProgress) return true;

        try
        {
            if (Main.Server.IsRunning || Main.Client.IsConnected)
            {
                if (source is "SlimeFeral.Awake")
                {
                    return false;
                }
            }
        }
        catch
        {
            // ignored
        }

        if (HandlingPacket || !actorObj)
            return true;

        if (source is "SR2MP.ActorDestroyHandler" or "SR2MP.InitialActors" or "SR2MP.RemoveExistingGadgetModel" or "SR2MP.ReplaceDrone")
            return true;

        var actor = actorObj.GetComponent<IdentifiableActor>();
        if (!actor)
            return true;
        
        // Drone destructions are handled differently
        if (actorObj.GetComponent<Il2CppMonomiPark.SlimeRancher.Drone.RanchDrone>() ||
            actorObj.GetComponent<Il2CppMonomiPark.SlimeRancher.Drone.ExplorerDrone>())
            return true;

        ActorManager.Actors.Remove(actor.GetActorId().Value);

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return true;

        try
        {
            var packet = new ActorDestroyPacket { ActorId = actor.GetActorId() };
            Main.SendToAllOrServer(packet);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send ActorDestroy packet: {ex.Message}");
        }

        return true;
    }
}