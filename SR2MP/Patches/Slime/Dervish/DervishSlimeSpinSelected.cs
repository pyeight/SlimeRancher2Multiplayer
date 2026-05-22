using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime.Dervish;
using MelonLoader;
using SR2MP.Components.Actor;
using SR2MP.Packets.Slime.Dervish;

namespace SR2MP.Patches.Slime.Dervish;

[HarmonyPatch(typeof(DervishSlimeSpin), nameof(DervishSlimeSpin.Selected))]
internal static class DervishSlimeSpinSelected
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

        if (!__instance._cyclone) return;

        var packet = new DervishCyclonePacket
        {
            ActorId = networkActor.ActorId,
            Active = true,
            Size = (byte)(int)__instance.GetCycloneSize(),
            FloatDir = __instance._floatDir
        };

        Main.SendToAllOrServer(packet);
    }
}