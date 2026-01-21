using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Map;

[HarmonyPatch(typeof(MapNodeActivator), nameof(MapNodeActivator.Activate))]
public static class OnMapUnlocked
{
    public static void Postfix(MapNodeActivator __instance)
    {
        var packet = new MapUnlockPacket
        {
            NodeID = __instance._fogRevealEvent._dataKey
        };
        Main.SendToAllOrServer(packet);
    }
}