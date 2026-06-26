using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Slime.Gold;

[HarmonyPatch(typeof(ProducePlortsOnHit), nameof(ProducePlortsOnHit.ProducePlort))]
internal static class GoldSlimeProducePlort
{
    public static bool Prefix(ProducePlortsOnHit __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;
        if (HandlingPacket) return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor != null && !networkActor.LocallyOwned)
            return false;

        return true;
    }
}