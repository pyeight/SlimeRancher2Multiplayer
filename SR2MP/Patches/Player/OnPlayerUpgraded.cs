using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Upgrade;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(UpgradeModel), nameof(UpgradeModel.IncrementUpgradeLevel))]
internal static class OnPlayerUpgraded
{
    public static void Postfix(UpgradeDefinition definition)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

        var packet = new PlayerUpgradePacket { UpgradeID = (byte)definition._uniqueId };

        Main.SendToAllOrServer(packet);
    }
}