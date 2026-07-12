using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Components.Drone;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkDroneManager
{
    internal static readonly Dictionary<long, string> PendingOwners = new();
    
    // Last tracked owner per Drone Station
    internal static readonly Dictionary<long, string> CachedOwners = new();

    private static bool subscribedToServerStart;
    private static bool initialized;

    internal static void CheckSubscribed()
    {
        if (subscribedToServerStart)
            return;

        Main.Server.OnServerStarted += OnServerStarted;
        subscribedToServerStart = true;
    }
    
    internal static void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        StartCoroutine(OwnUnownedDronesLoop());
    }

    private static IEnumerator OwnUnownedDronesLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            TakeOwnershipOfNearby();
        }
    }
    
    private static void TakeOwnershipOfNearby()
    {
        if (SceneContext.Instance?.player == null)
            return;

        var bounds = new Bounds(SceneContext.Instance.player.transform.position, new Vector3(600, 1250, 600));

        foreach (var drone in NetworkDrone.Drones.Values.ToArray())
        {
            try
            {
                if (!drone || drone.IsHibernated || drone.LocallyOwned)
                    continue;

                if (!bounds.Contains(drone.transform.position))
                    continue;

                var ownerId = drone.CurrentOwnerId;
                if (!string.IsNullOrEmpty(ownerId) && PlayerManager.CheckPlayerExists(ownerId))
                    continue;

                drone.ClaimOwnership();
            }
            catch { /* ignored */ }
        }
    }

    private static void OnServerStarted()
    {
        foreach (var drone in NetworkActorManager.GetAllOfType<RanchDroneModel>())
        {
            var ranchDrone = GetNetworkComponent(drone.GetGameObject());
            ranchDrone.LocallyOwned = true;
            ranchDrone.CurrentOwnerId = Main.Server.PlayerId;
        }
        
        foreach (var drone in NetworkActorManager.GetAllOfType<ExplorerDroneModel>())
        {
            var explorerDrone = GetNetworkComponent(drone.GetGameObject());
            explorerDrone.LocallyOwned = true;
            explorerDrone.CurrentOwnerId = Main.Server.PlayerId;
        }
    }

    internal static NetworkDrone GetNetworkComponent(GameObject droneObject)
        => droneObject.GetOrAddComponent<NetworkDrone>();

    internal static bool IsDroneModel(IdentifiableModel model)
        => model.TryCast<RanchDroneModel>() != null || model.TryCast<ExplorerDroneModel>() != null;

    internal static void RemoveStationDrone(ActorId stationId)
    {
        var allDrones = GameState.droneModel;

        if (allDrones.TryGetRanchDroneByStationId(stationId, out var ranchModel) && ranchModel != null)
            DestroyDrone(ranchModel.TryCast<IdentifiableModel>());
        allDrones._ranchDrones.Remove(stationId);

        if (allDrones.TryGetExplorerDroneByStationId(stationId, out var explorerModel) && explorerModel != null)
            DestroyDrone(explorerModel.TryCast<IdentifiableModel>());
        allDrones._explorerDrones.Remove(stationId);
        
        if (NetworkDrone.Drones.TryGetValue(stationId.Value, out var networkDrone) && networkDrone)
        {
            HandlingPacket = true;
            try { Destroyer.DestroyAny(networkDrone.gameObject, "SR2MP.ReplaceDrone"); }
            catch { /* ignored */ }
            HandlingPacket = false;
        }

        NetworkDrone.Drones.Remove(stationId.Value);
    }

    private static void DestroyDrone(IdentifiableModel? model)
    {
        if (model == null)
            return;

        HandlingPacket = true;

        try
        {
            var gameObj = model.GetGameObject();

            if (gameObj)
                Destroyer.DestroyAny(gameObj, "SR2MP.ReplaceDrone");
            else
                GameState.DestroyIdentifiableModel(model);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to destroy drone {model.actorId.Value}: {ex.Message}");
        }

        HandlingPacket = false;

        ActorManager.Actors.Remove(model.actorId.Value);
    }

    internal static void AssignOwnershipOfUnowned()
    {
        if (!Main.Server.IsRunning)
            return;

        if (SceneContext.Instance?.player == null)
            return;

        var allPlayers = new List<(string PlayerId, Vector3 Position)>
        {
            (LocalID, SceneContext.Instance.player.transform.position)
        };

        foreach (var (playerId, playerObject) in PlayerObjects)
        {
            if (playerObject)
                allPlayers.Add((playerId, playerObject.transform.position));
        }

        foreach (var (stationId, drone) in NetworkDrone.Drones.ToArray())
        {
            try
            {
                if (!drone || drone.IsHibernated)
                    continue;

                if (drone.CurrentOwnerId == LocalID || PlayerManager.CheckPlayerExists(drone.CurrentOwnerId))
                    continue;

                var position = drone.transform.position;
                var newOwner = LocalID;
                var bestDistance = float.MaxValue;

                foreach (var (playerId, playerPosition) in allPlayers)
                {
                    var distance = (playerPosition - position).sqrMagnitude;

                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    newOwner = playerId;
                }

                drone.CurrentOwnerId = newOwner;
                drone.LocallyOwned = newOwner == LocalID;
                CachedOwners[stationId] = newOwner;

                Main.SendToAllOrServer(new Packets.Drone.DroneOwnershipPacket
                {
                    StationId = stationId,
                    ClaimerID = newOwner,
                    PreviousOwnerID = string.Empty
                });
            }
            catch { /* ignored */ }
        }
    }
}
