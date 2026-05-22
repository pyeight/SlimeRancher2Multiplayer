using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime.Slumber;
using SR2MP.Components.Actor;
using SR2MP.Packets.Slime.Sloomber;

namespace SR2MP.Patches.Slime.Sloomber;

[HarmonyPatch(typeof(InitiateYawn), nameof(InitiateYawn.DoYawn))]
internal static class InitiateYawnDoYawn
{
    public static void Postfix(InitiateYawn __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (HandlingPacket) return;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (!networkActor.LocallyOwned) return;

        Main.SendToAllOrServer(new SloomberYawnPacket { ActorId = networkActor.ActorId, Active = false });
    }
}