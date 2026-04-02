using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo;

[HarmonyPatch(typeof(AmmoSlot), nameof(AmmoSlot.DecrementAmmo))]
public static class OnAmmoSlotDecrement
{
    public static void Postfix(AmmoSlot __instance)
    {
        if ((!Main.Client.IsConnected && !Main.Server.IsRunning()) || handlingPacket) return;

        if (__instance._count <= 0) __instance._id = null;

        var index = __instance.GetSlotIndex();
        if (index == null) return;
        
        var packet = new AmmoDecrementPacket()
        {
            SlotIndex = (int)index,
            Count = 1,
            ID = __instance.GetPlotID()!
        };

        if (packet.ID == null) return;

        Main.SendToAllOrServer(packet);
    }
}