using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.Plots;

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.DestroyAttached))]
public static class OnDestroyCrop
{
    public static void Postfix(LandPlot __instance)
    {
        if (handlingPacket) return;

        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

        if (!__instance)
            return;

        var packet = new GardenPlantPacket()
        {
            ID = __instance.GetComponentInParent<LandPlotLocation>()._id,
            ActorType = 9
        };

        Main.SendToAllOrServer(packet);
    }
}