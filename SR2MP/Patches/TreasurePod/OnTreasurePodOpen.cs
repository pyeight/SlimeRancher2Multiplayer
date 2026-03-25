using HarmonyLib;
using SR2MP.Packets.TreasurePod;

namespace SR2MP.Patches.TreasurePod;

[HarmonyPatch(typeof(Il2Cpp.TreasurePod), nameof(Il2Cpp.TreasurePod.Activate))]
internal static class OnTreasurePodOpen
{
    public static void Postfix(Il2Cpp.TreasurePod __instance)
    {
        if (HandlingPacket)
            return;

        var packet = new TreasurePodPacket()
        {
            ID = int.Parse(__instance._id.Replace("pod", string.Empty))
        };

        Main.SendToAllOrServer(packet);
    }
}