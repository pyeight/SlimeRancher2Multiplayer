using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2E.Utils;
using SR2MP.Components.Actor;

namespace SR2MP.Shared.Managers;

public sealed class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors              = new();
    public readonly Dictionary<int, IdentifiableType> ActorTypes            = new();

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

    internal IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad(true);

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
                if (obj)
                {
                    Object.Destroy(obj);
                    Actors.Remove(actor.value.actorId.Value);
                }
            }

            foreach (var actor2 in gameModel!.identifiables)
            {
                if (actor2.value.ident.IsPlayer)
                    continue;

                var model = actor2.value.TryCast<ActorModel>();

                if (model == null)
                    continue;

                if (!model.ident.prefab)
                    continue;

                if (actor2.value.sceneGroup == scene)
                {
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
            }
        }
    }

    private bool ActorIDAlreadyInUse(ActorId id)
    {
        var gameModel = SceneContext.Instance?.GameModel;

        if (!gameModel)
        {
            return false;
        }

        return gameModel!.TryGetIdentifiableModel(id, out _);
    }

    public bool TrySpawnNetworkActor(ActorId actorId, Vector3 position, Quaternion rotation, int typeId, int sceneId, out ActorModel? actorModel)
    {
        actorModel = null;


        if (Main.RockPlortBug)
            typeId = 25;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorId.Value}: type_{typeId}");
            return false;
        }

        if (!type.prefab)
            return false;

        if (type.isGadget())
        {
            SrLogger.LogWarning($"Tried to spawn gadget over the network, this has not been implemented yet!\n\tActor {actorId.Value}: {type.name}");
            return false;
        }

        if (ActorIDAlreadyInUse(actorId))
            return false;

        actorModel = SceneContext.Instance.GameModel.CreateActorModel(
                actorId,
                type,
                scene,
                position,
                rotation);

        if (actorModel == null)
            return false;

        SceneContext.Instance.GameModel.identifiables[actorId] = actorModel;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(actorModel);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(actorModel);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(actorModel);
        handlingPacket = false;

        if (actor)
        {
            var networkComponent = actor.AddComponent<NetworkActor>();
            networkComponent.previousPosition = position;
            networkComponent.nextPosition = position;
            networkComponent.previousRotation = rotation;
            networkComponent.nextRotation = rotation;
            actor.transform.position = position;
            actorManager.Actors[actorId.Value] = actorModel;
        }

        return true;
    }

    public static long GetHighestActorIdInRange(long min, long max)
    {
        long result = min;
        foreach (var actor in SceneContext.Instance.GameModel.identifiables)
        {
            var id = actor.value.actorId.Value;
            if (id >= min && id < max)
            {
                if (id > result)
                {
                    result = id;
                }
            }
        }
        return result;
    }
}