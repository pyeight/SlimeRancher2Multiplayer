using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(DroneStationGadgetModel), nameof(DroneStationGadgetModel.SetTaskData))]
internal static class DroneSetTaskData
{
    public static void Postfix(DroneStationGadgetModel __instance, DroneTaskData newData)
    {
        if (HandlingPacket)
            return;

        var targetIdent = (newData.TargetIdentType != null) ? NetworkActorManager.GetPersistentID(newData.TargetIdentType) : -1;

        var packet = new DroneProgramPacket
        {
            ActorId = __instance.actorId.Value,
            TargetIdent = targetIdent,
            Target = (byte)newData.TargetType,
            Sink = (byte)newData.SinkType,
            Source = (byte)newData.SourceType
        };

        Main.SendToAllOrServer(packet);
    }
}
