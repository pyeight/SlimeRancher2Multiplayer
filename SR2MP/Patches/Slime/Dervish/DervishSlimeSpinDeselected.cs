using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime.Dervish;
using SR2MP.Components.Actor;
using SR2MP.Packets.Slime.Dervish;

namespace SR2MP.Patches.Slime.Dervish;

[HarmonyPatch(typeof(DervishSlimeSpin), nameof(DervishSlimeSpin.Deselected))]
internal static class DervishSlimeSpinDeselected
{
    public static bool Prefix(DervishSlimeSpin __instance)
    {
        var networkActor = __instance.GetComponent<NetworkActor>();
        if (!networkActor.LocallyOwned && !HandlingPacket) return false;

        return true;
    }
    
    public static void Postfix(DervishSlimeSpin __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (HandlingPacket) return;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (!networkActor.LocallyOwned) return;

        var packet = new DervishCyclonePacket
        {
            ActorId = networkActor.ActorId,
            Active = false
        };

        Main.SendToAllOrServer(packet);
    }
}