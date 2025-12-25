using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Plots;

[HarmonyPatch(typeof(LandPlot), nameof(Il2Cpp.LandPlot.AddUpgrade))]
public static class UpgradeLandPlot
{
    public static void Postfix(LandPlot __instance, LandPlot.Upgrade upgrade)
    {
        if (handlingPacket) return;
        
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

        if (!__instance)
            return;

        var packet = new LandPlotUpdatePacket()
        {
            Type = (byte)PacketType.LandPlotUpdate,
            ID = __instance.GetComponentInParent<LandPlotLocation>()._id,
            PlotUpgrade = upgrade,
            IsUpgrade = true
        };

        Main.SendToAllOrServer(packet);
    }
}