using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.Player.Component;
using SR2MP.Packets.Upgrade;

namespace SR2MP.Patches.Refinery;

[HarmonyPatch(typeof(UpgradeComponentsModel), nameof(UpgradeComponentsModel.LoseComponent))]
internal static class OnComponentLost
{
    public static void Postfix(UpgradeComponentsModel __instance, UpgradeComponent upgradeComponent)
    {
        if (HandlingPacket) return;

        var packet = new UpgradeComponentPacket
        {
            ComponentId = upgradeComponent._referenceId,
            Count = (byte)__instance.GetComponentQuantity(upgradeComponent)
        };

        Main.SendToAllOrServer(packet);
    }
}
