using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(ResourceCycle), nameof(ResourceCycle.Attach))]
internal static class ResourceCycleAttachPatch
{
    public static void Prefix(ResourceCycle __instance, Joint joint)
    {
        if (HandlingPacket) return;
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;
        if (joint == null) return;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor == null || !networkActor.LocallyOwned) return;

        var actorId = networkActor.ActorId;
        if (actorId.Value == 0) return;

        var spawner = joint.gameObject.GetComponentInParent<SpawnResource>();
        if (spawner == null) return;

        var jointIndex = spawner.SpawnJoints.IndexOf(joint);
        if (jointIndex < 0) return;

        var plotId = joint.gameObject.GetComponentInParent<LandPlotLocation>()?._id ?? string.Empty;

        var spawnModel = spawner._model ?? new SpawnResourceModel();

        var packet = new ResourceAttachPacket
        {
            ActorId = actorId,
            PlotID = plotId,
            Joint = jointIndex,
            SpawnerID = spawner.transform.position,
            Model = spawnModel
        };

        Main.SendToAllOrServer(packet);
    }
}