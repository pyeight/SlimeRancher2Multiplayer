using HarmonyLib;
using SR2MP.Packets.Actor;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(ResourceCycle), nameof(ResourceCycle.Attach))]
public static class OnResourceAttach
{
    public static void Prefix(ResourceCycle __instance, Joint joint)
    {
        if (handlingPacket) return;

        if (joint.connectedBody)
        {
            var other = joint.connectedBody.GetComponent<ResourceCycle>();

            SceneContext.Instance.GameModel.identifiables.Remove(other._model.actorId);
            SceneContext.Instance.GameModel.identifiablesByIdent[other._model.ident].Remove(other._model);
            SceneContext.Instance.GameModel.DestroyIdentifiableModel(other._model);

            Destroyer.DestroyActor(other.gameObject, "SR2MP.OnResourceAttach");
            joint.connectedBody = null;
            return;
        }

        var spawner = joint.gameObject.GetComponentInParent<SpawnResource>();
        if (spawner)
        {
            var index = spawner.SpawnJoints.IndexOf(joint);

            var id = joint.gameObject.GetComponentInParent<LandPlotLocation>()?._id ?? "";

            var packet = new ResourceAttachPacket()
            {
                ActorId = __instance._model.actorId,
                Joint = index,
                PlotID = id,
                SpawnerID = spawner.transform.position,
                Model = spawner._model,
            };

            // Main.SendToAllOrServer(packet);
        }
    }
}