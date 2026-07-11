using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(DroneStationGadgetModel), nameof(DroneStationGadgetModel.AddEnergy))]
internal static class DroneAddEnergy
{
    public static void Postfix(DroneStationGadgetModel __instance, TimeDirector timeDir, float energyDepletedPerHour)
    {
        if (HandlingPacket) return;

        var packet = new DroneBatteryPacket
        {
            ActorId = __instance.actorId.Value,
            Charge = __instance.GetCurrEnergy(timeDir, energyDepletedPerHour),
            EnergyDepletedPerHour = energyDepletedPerHour
        };

        Main.SendToAllOrServer(packet);
    }
}
