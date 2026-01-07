using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
public static class OnGameLoadPatch
{
    public static void Postfix()
    {
        Main.Server.OnServerStarted += () =>
        {
            foreach (var actor in SceneContext.Instance.GameModel.identifiables)
            {
                if (actor.Value.TryCast<ActorModel>() == null) continue;

                var transform = actor.value.Transform;

                if (!transform)
                    continue;
                var networkComponent = transform.GetComponent<NetworkActor>();

                if (networkComponent) continue;

                transform.gameObject.AddComponent<NetworkActor>().LocallyOwned = true;

                actorManager.Actors.Add(transform.GetComponent<Identifiable>().GetActorId().Value, actor.value);
            }
        };
    }
}