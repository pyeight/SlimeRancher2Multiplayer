using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors = new();
    public readonly Dictionary<int, IdentifiableType> ActorTypes = new();

    public static int GetPersistentID(IdentifiableType type)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);

    internal void Initialize(GameContext context)
    {
        ActorTypes.Clear();
        Actors.Clear();

        foreach (var type in context.AutoSaveDirector._saveReferenceTranslation._identifiableTypeLookup)
            ActorTypes.TryAdd(GetPersistentID(type.value), type.value);

        ActorTypes[-1] = null!;

        StartCoroutine(ZoneLoadingLoop());
        StartCoroutine(OwnUnownedSlimesLoop());
    }

    private IEnumerator OwnUnownedSlimesLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            if (SceneContext.Instance?.player == null)
                continue;

            TakeOwnershipOfNearby(true);
        }
    }

    private IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad();

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
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

                HandlingPacket = true;
                var obj = InstantiationHelpers.InstantiateActorFromModel(model);
                HandlingPacket = false;

                if (!obj)
                    continue;

                var networkComponent = obj.AddComponent<NetworkActor>();

                networkComponent.previousPosition = model.lastPosition;
                networkComponent.nextPosition     = model.lastPosition;
                networkComponent.previousRotation = model.lastRotation;
                networkComponent.nextRotation     = model.lastRotation;

                Actors.Add(model.actorId.Value, model);
            }

            TakeOwnershipOfNearby();
        }
    }

    private static bool ActorIDAlreadyInUse(ActorId id)
        => SceneContext.Instance?.GameModel?.TryGetIdentifiableModel(id, out _) ?? false;

    public static long GetHighestActorIdInRange(long min, long max)
    {
        var result = min;
        foreach (var actor in GameState.identifiables)
        {
            var id = actor.value.actorId.Value;
            if (id < min || id >= max)
                continue;
            if (id > result)
                result = id;
        }

        return result;
    }
    
    internal void TakeOwnershipOfNearby(bool onlyUnownedSlimes = false)
    {
        var bounds = new Bounds(SceneContext.Instance.player.transform.position, new Vector3(600, 1250, 600));

        foreach (var actor in Actors.Values.ToArray())
        {
            try
            {
                if (actor == null)
                    continue;

                if (!bounds.Contains(actor.lastPosition))
                    continue;

                if (!actor.TryGetNetworkComponent(out var netActor))
                    continue;

                if (onlyUnownedSlimes)
                {
                    if (netActor.LocallyOwned)
                        continue;

                    if (netActor.RegionMember?._hibernating == true)
                        continue;

                    var ownerId = netActor.CurrentOwnerId;
                    if (!string.IsNullOrEmpty(ownerId) && PlayerManager.CheckPlayerExists(ownerId))
                        continue;

                    if (!netActor.isSlime)
                        continue;
                }

                if (onlyUnownedSlimes)
                    SrLogger.LogMessage("owning unowned slime");

                var actorId = netActor.ActorId;
                if (actorId.Value == 0)
                    continue;

                var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID };
                ApplyOwnership(packet);
                Main.SendToAllOrServer(packet);
            }
            catch { /* ignored */ }
        }
    }
    
    internal void AssignOwnershipOfUnowned()
    {
        if (SceneContext.Instance?.player == null)
            return;

        var bounds = new Vector3(600, 1250, 600);

        var allPlayers = new List<(string PlayerId, Vector3 Position)>
        {
            (LocalID, SceneContext.Instance.player.transform.position)
        };

        foreach (var (playerId, playerObject) in PlayerObjects)
        {
            if (playerObject)
                allPlayers.Add((playerId, playerObject.transform.position));
        }

        foreach (var actor in Actors.Values.ToArray())
        {
            try
            {
                if (actor == null)
                    continue;

                if (!actor.TryGetNetworkComponent(out var netActor))
                    continue;

                if (netActor.CurrentOwnerId == LocalID || PlayerManager.CheckPlayerExists(netActor.CurrentOwnerId))
                    continue;

                var actorId = netActor.ActorId;
                if (actorId.Value == 0)
                    continue;

                var position = actor.lastPosition;
                string? newOwner = null;
                var bestDistance = float.MaxValue;

                foreach (var (playerId, playerPosition) in allPlayers)
                {
                    if (!new Bounds(playerPosition, bounds).Contains(position))
                        continue;

                    var distance = (playerPosition - position).sqrMagnitude;

                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    newOwner = playerId;
                }

                if (newOwner == null)
                    continue;

                var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = newOwner };
                ApplyOwnership(packet);
                Main.SendToAllOrServer(packet);
            }
            catch { /* ignored */ }
        }
    }
}