using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;

namespace SR2MP.Shared.Managers;

public sealed partial class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors    = new();
    public readonly Dictionary<int, IdentifiableType> ActorTypes  = new();

    public static int GetPersistentID(IdentifiableType type)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);

    internal void Initialize(GameContext context)
    {
        ActorTypes.Clear();
        Actors.Clear();

        foreach (var type in context.AutoSaveDirector._saveReferenceTranslation._identifiableTypeLookup)
        {
            ActorTypes.TryAdd(GetPersistentID(type.value), type.value);
        }

        MelonCoroutines.Start(ZoneLoadingLoop());
    }

    private IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad();

            if (!Main.Server.IsRunning() && !Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            var gameModel = SceneContext.Instance?.GameModel;
            if (!gameModel)
                continue;

            var scene = SystemContext.Instance.SceneLoader.CurrentSceneGroup;

            foreach (var actor in gameModel!.identifiables)
            {
                if (actor.value.ident.IsPlayer)
                    continue;

                if (actor.value.TryCast<ActorModel>() == null)
                    continue;

                var obj = actor.value.GetGameObject();
                if (!obj)
                    continue;
                Object.Destroy(obj);
                Actors.Remove(actor.value.actorId.Value);
            }

            foreach (var actor2 in gameModel.identifiables)
            {
                if (actor2.value.ident.IsPlayer)
                    continue;

                var model = actor2.value.TryCast<ActorModel>();

                if (model == null)
                    continue;

                if (!model.ident.prefab)
                    continue;

                if (actor2.value.sceneGroup != scene)
                    continue;
                handlingPacket = true;
                var obj = InstantiationHelpers.InstantiateActorFromModel(model);
                handlingPacket = false;

                if (!obj)
                    continue;

                var networkComponent = obj.AddComponent<NetworkActor>();

                networkComponent.previousPosition = model.lastPosition;
                networkComponent.nextPosition = model.lastPosition;
                networkComponent.previousRotation = model.lastRotation;
                networkComponent.nextRotation = model.lastRotation;

                actorManager.Actors.Add(model.actorId.Value, model);
            }

            yield return TakeOwnershipOfNearby();
        }
    }

    private static bool ActorIDAlreadyInUse(ActorId id)
    {
        var gameModel = SceneContext.Instance?.GameModel;
        return gameModel && gameModel!.TryGetIdentifiableModel(id, out _);
    }

    public static long GetHighestActorIdInRange(long min, long max)
    {
        long result = min;
        foreach (var actor in SceneContext.Instance.GameModel.identifiables)
        {
            var id = actor.value.actorId.Value;
            if (id < min || id >= max)
                continue;
            if (id > result)
            {
                result = id;
            }
        }
        return result;
    }

    internal IEnumerator TakeOwnershipOfNearby()
    {
        const int Max = 12;

        var player = SceneContext.Instance.player;

        var bounds = new Bounds(player.transform.position, new Vector3(325, 1000, 325));

        int i = 0;
        foreach (var actor in Actors)
        {
            if (actor.Value == null)
                continue;

            if (!bounds.Contains(actor.Value.lastPosition))
                continue;

            if (actor.Value.TryGetNetworkComponent(out var netActor))
                continue;

            if (netActor == null)
                continue;
            
            netActor.LocallyOwned = true;

            var actorId = netActor.ActorId;
            if (actorId.Value == 0)
            {
                yield break;
            }

            var packet = new ActorTransferPacket
            {
                ActorId = actorId,
                OwnerId = LocalID,
            };
            Main.SendToAllOrServer(packet);
            i++;

            if (i > Max)
            {
                yield return null;
                i = 0;
            }
        }
    }
}