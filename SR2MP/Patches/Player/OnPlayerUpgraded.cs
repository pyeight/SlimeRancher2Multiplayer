using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Upgrades;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(UpgradeModel), nameof(UpgradeModel.IncrementUpgradeLevel))]
public static class OnPlayerUpgraded
{
    public static void Postfix(UpgradeDefinition definition)
    {
        if (handlingPacket) return;

        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

        var packet = new PlayerUpgradePacket { UpgradeID = (byte)definition._uniqueId };

        Main.SendToAllOrServer(packet);
    }
}