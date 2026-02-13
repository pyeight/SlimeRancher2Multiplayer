using HarmonyLib;
using SR2MP.Packets.LandPlots;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Plots;

[HarmonyPatch(typeof(GardenCatcher), nameof(GardenCatcher.Plant))]
public static class PlantGarden
{
    public static void Postfix(GardenCatcher __instance, IdentifiableType cropId)
    {
        if (handlingPacket)
            return;

        var packet = new GardenPlantPacket
        {
            ActorType = NetworkActorManager.GetPersistentID(cropId),
            ID = __instance.GetComponentInParent<LandPlotLocation>()._id
        };

        Main.SendToAllOrServer(packet);
    }
}