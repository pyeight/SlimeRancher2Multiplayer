using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(UpgradeModel), nameof(UpgradeModel.IncrementUpgradeLevel))]
public static class OnPlayerUpgraded
{
    public static void Postfix(UpgradeModel __instance, UpgradeDefinition definition)
    {
        if (handlingPacket) return;
        
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;
            
        var packet = new PlayerUpgradePacket()
        {
            Type = (byte)PacketType.PlayerUpgrade,
            UpgradeID = (byte)definition._uniqueId
        };

        Main.SendToAllOrServer(packet);
    }
}