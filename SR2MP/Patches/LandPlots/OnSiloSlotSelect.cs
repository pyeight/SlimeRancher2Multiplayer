using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(SiloStorageActivator), nameof(SiloStorageActivator.Activate))]
public static class OnSiloSlotSelect
{
    public static void Postfix(SiloStorageActivator __instance)
    {
        if (HandlingPacket) return;

        var packet = new SiloSlotSelectPacket()
        {
            ID = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id,
            Side = (byte)__instance.ActivatorIdx,
            Index = (byte)__instance._landPlotModel.siloStorageIndices[__instance.ActivatorIdx]
        };

        Main.SendToAllOrServer(packet);
    }
}