using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(PlortCollector), nameof(PlortCollector.StartCollection))]
public static class OnPlortCollection
{
    public static void Postfix(PlortCollector __instance)
    {
        if (HandlingPacket) return;

        var packet = new PlortCollectionPacket()
        {
            ID = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id,
            EndTime = __instance._endCollectAt,
        };

        Main.SendToAllOrServer(packet);
    }
}