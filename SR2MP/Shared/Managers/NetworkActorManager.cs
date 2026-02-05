using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2E.Utils;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

public sealed class NetworkActorManager
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

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = actorModel;

        return true;
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

            netActor.LocallyOwned = true;

            var actorId = netActor.ActorId;
            if (actorId.Value == 0)
            {
                yield break;
            }

            var packet = new ActorTransferPacket
            {
                ActorId = actorId,
                OwnerPlayer = LocalID,
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

    public static InitialActorsPacket.ActorBase CreateInitialActor(IdentifiableModel actor)
    {
        if (actor.TryCast<SlimeModel>(out var slime))
            return CreateInitialSlime(slime);

        if (actor.TryCast<PlortModel>(out var plort))
            return CreateInitialPlort(plort);

        if (actor.TryCast<ResourceModel>(out var resource))
            return CreateInitialResource(resource);

        return CreateInitialActorBase(actor);
    }

    private static InitialActorsPacket.ActorBase CreateInitialActorBase(IdentifiableModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.TryCast<ActorModel>()?.lastRotation ?? Quaternion.identity,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup)
    };

    private static InitialActorsPacket.Slime CreateInitialSlime(SlimeModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        Emotions = model.Emotions
    };

    private static InitialActorsPacket.Plort CreateInitialPlort(PlortModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        DestroyTime = model.destroyTime,
        Invulnerable = model._invulnerability.IsInvulnerable,
        InvulnerablePeriod = model._invulnerability.InvulnerabilityPeriod
    };

    private static InitialActorsPacket.Resource CreateInitialResource(ResourceModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        DestroyTime = model.destroyTime
    };

    public bool TrySpawnInitialActor(InitialActorsPacket.ActorBase actorData, out IdentifiableModel? model)
    {
        model = null;

        if (actorData is InitialActorsPacket.Slime slimeData)
            return TrySpawnInitialSlime(slimeData, out model);

        var sceneId = actorData.Scene;
        var typeId = actorData.ActorTypeId;
        var actorId = new ActorId(actorData.ActorId);

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

        var position = actorData.Position;
        var rotation = actorData.Rotation;

        model = SceneContext.Instance.GameModel.CreateActorModel(
                actorId,
                type,
                scene,
                position,
                rotation);

        if (model == null)
            return false;

        SceneContext.Instance.GameModel.identifiables[actorId] = model;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        handlingPacket = false;

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        return true;
    }

    private bool TrySpawnInitialSlime(InitialActorsPacket.Slime actorData, out IdentifiableModel? model)
    {
        model = null;

        var sceneId = actorData.Scene;
        var typeId = actorData.ActorTypeId;
        var actorId = new ActorId(actorData.ActorId);

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

        if (ActorIDAlreadyInUse(actorId))
            return false;

        var position = actorData.Position;
        var rotation = actorData.Rotation;
        var emotions = actorData.Emotions;

        model = SceneContext.Instance.GameModel.CreateSlimeActorModel(
                actorId,
                type.Cast<SlimeDefinition>(),
                scene,
                position,
                rotation);

        if (model == null)
            return false;

        model.Cast<SlimeModel>().Emotions = emotions;

        SceneContext.Instance.GameModel.identifiables[actorId] = model;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        handlingPacket = false;

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        return true;
    }
}