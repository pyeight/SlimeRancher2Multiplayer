using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
public static class OnGameLoadPatch
{
    public static void Postfix() => Main.Server.OnServerStarted += () =>
    {
        foreach (var actor in GameState.identifiables)
        {
            if (actor.value.TryCast<ActorModel>() == null) continue;

            var transform = actor.value.Transform;

            if (!transform)
                continue;
            var networkComponent = transform.GetComponent<NetworkActor>();

            if (networkComponent) continue;

            transform.gameObject.AddComponent<NetworkActor>().LocallyOwned = true;

            actorManager.Actors[actor.value.actorId.Value] = actor.value;
        }

        GameState._actorIdProvider._nextActorId =
            NetworkActorManager.GetHighestActorIdInRange(0, 100000);
    };
}