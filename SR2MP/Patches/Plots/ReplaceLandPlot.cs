using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Plots;

[HarmonyPatch(typeof(LandPlotLocation), nameof(LandPlotLocation.Replace))]
public static class ReplaceLandPlot
{
    public static void Postfix(LandPlotLocation __instance, LandPlot oldLandPlot, GameObject replacementPrefab)
    {
        if (handlingPacket) return;
        
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;
            
        var packet = new LandPlotUpdatePacket()
        {
            Type = (byte)PacketType.LandPlotUpdate,
            ID = __instance._id,
            IsUpgrade = false,
            PlotType = replacementPrefab.GetComponent<LandPlot>().TypeId
        };

        Main.SendToAllOrServer(packet);
    }
}