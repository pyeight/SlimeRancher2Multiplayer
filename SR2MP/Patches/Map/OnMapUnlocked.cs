using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Map;

[HarmonyPatch(typeof(MapNodeActivator), nameof(MapNodeActivator.Activate))]
internal static class OnMapUnlocked
{
    public static void Postfix(MapNodeActivator __instance)
    {
        var packet = new MapUnlockPacket
        {
            // NodeID = __instance._fogRevealEvent._dataKey,
            NodeID =__instance._fogRevealEvent.DataKey
        };

        Main.SendToAllOrServer(packet);
    }
}