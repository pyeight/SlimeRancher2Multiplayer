using HarmonyLib;
using SR2MP.Components.LandPlots;
using SR2MP.Packets.LandPlots;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(GardenCatcher), nameof(GardenCatcher.Plant))]
internal static class PlantGarden
{
    public static void Postfix(GardenCatcher __instance, IdentifiableType cropId, GameObject __result)
    {
        if (HandlingPacket)
            return;

        var packet = new GardenPlantPacket
        {
            ActorType = NetworkActorManager.GetPersistentID(cropId),
            ID = __instance.GetComponentInParent<LandPlotLocation>()._id
        };

        Main.SendToAllOrServer(packet);

        if (__result)
            __result.GetComponent<NetworkGarden>()?.ClaimOnReady();
    }
}