using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(Destroyer), nameof(Destroyer.DestroyActor), typeof(GameObject), typeof(string), typeof(bool))]
public static class OnActorDestroy
{
    public static bool Prefix(GameObject actorObj, string source, bool okIfNonActor)
    {
        if (SystemContext.Instance.SceneLoader.IsSceneLoadInProgress) return true;
        
        try
        {
            if (Main.Server.IsRunning() || Main.Client.IsConnected)
            {
                if (source.Equals("ResourceCycle.RegistryUpdate#1"))
                {
                    return false;
                }
                if (source.Equals("SlimeFeral.Awake"))
                {
                    return false;
                }
            }
        }
        catch { }

        if ((Main.Server.IsRunning() || Main.Client.IsConnected) && !handlingPacket && actorObj)
        {
            var actor = actorObj.GetComponent<IdentifiableActor>();
            if (actor)
            {
                try
                {
                    var packet = new ActorDestroyPacket()
                    {
                        Type = (byte)PacketType.ActorDestroy,
                        ActorId = actor.GetActorId(),
                    };
                    Main.SendToAllOrServer(packet);
                }
                catch (Exception ex)
                {
                    SrLogger.LogError($"Failed to send ActorDestroy packet: {ex.Message}");
                }
            }
        }
        return true;
    }
}