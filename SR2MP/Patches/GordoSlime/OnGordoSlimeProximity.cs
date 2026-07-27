using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Packets.GordoSlime;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoPlayerProximityCheck), nameof(GordoPlayerProximityCheck.OnTriggerEnter))]
internal static class OnGordoSlimeProximity
{
    public static void Prefix(GordoPlayerProximityCheck __instance, out bool __state)
        => __state = __instance._model != null && __instance._model.GordoSeen;

    public static void Postfix(GordoPlayerProximityCheck __instance, bool __state)
    {
        if (HandlingPacket) return;

        var model = __instance._model;
        if (model == null) return;

        if (__state) return;
        if (!model.GordoSeen) return;

        var id = NetworkGordoSlimeManager.ResolveGordoSlimeId(model);
        if (string.IsNullOrEmpty(id)) return;

        Main.SendToAllOrServer(new GordoSlimeSeenPacket { ID = id });
    }
}
