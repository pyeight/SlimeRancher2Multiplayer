using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo;

[HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.Decrement), typeof(int), typeof(int))]
internal static class OnAmmoDecrement
{
    public static void Postfix(AmmoSlotManager __instance, int index, int count)
    {
        if ((!Main.Client.IsConnected && !Main.Server.IsRunning) || HandlingPacket) return;

        if (__instance.Slots[index]!._count <= 0) __instance.Slots[index]!._id = null;

        var packet = new AmmoDecrementPacket()
        {
            SlotIndex = index,
            Count = count,
            ID = __instance.GetPlotID()
        };

        if (packet.ID == null) return;

        Main.SendToAllOrServer(packet);
    }
}