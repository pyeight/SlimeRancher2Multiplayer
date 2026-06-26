using HarmonyLib;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.OnDestroy))]
internal static class OnLandPlotDestroy
{
    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        SrLogger.LogDebug("Suppressed exception: LandPlot.OnDestroy");
        return null;
    }
}
