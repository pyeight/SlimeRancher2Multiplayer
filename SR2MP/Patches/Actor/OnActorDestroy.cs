using HarmonyLib;
using SR2MP.Packets.Actor;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(Destroyer), nameof(Destroyer.DestroyActor), typeof(GameObject), typeof(string), typeof(bool))]
public static class OnActorDestroy
{
    public static bool Prefix(GameObject actorObj, string source)
    {
        if (SystemContext.Instance.SceneLoader.IsSceneLoadInProgress) return true;

        try
        {
            if (Main.Server.IsRunning() || Main.Client.IsConnected)
            {
                if (source is "ResourceCycle.RegistryUpdate#1" or "SlimeFeral.Awake")
                {
                    return false;
                }
            }
        }
        catch { }

        if ((!Main.Server.IsRunning() && !Main.Client.IsConnected) || handlingPacket || !actorObj)
            return true;

        var actor = actorObj.GetComponent<IdentifiableActor>();
        if (!actor)
            return true;

        actorManager.Actors.Remove(actor.GetActorId().Value);
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