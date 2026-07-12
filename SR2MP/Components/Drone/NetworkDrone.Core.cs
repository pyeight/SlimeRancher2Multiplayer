using Il2CppMonomiPark.SlimeRancher.Drone;
using Il2CppMonomiPark.SlimeRancher.Regions;
using JetBrains.Annotations;
using SR2MP.Packets.Drone;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using Starlight.Storage;

namespace SR2MP.Components.Drone;

[InjectIntoIL]
internal sealed partial class NetworkDrone : MonoBehaviour
{
    public static readonly Dictionary<long, NetworkDrone> Drones = new();

    private RanchDrone? ranchDrone;
    private ExplorerDrone? explorerDrone;
    private ExplorerDroneMovementManager? explorerMovement;
    private RegionMember? regionMember;
    private Rigidbody? rigidbody;

    public bool LocallyOwned { get; set; }
    public bool IsHibernated { get; private set; }
    public string CurrentOwnerId { get; set; } = string.Empty;
    public string CachedOwnerID { get; set; } = string.Empty;

    private long stationId;
    private bool registered;
    private bool pendingInitialClaim;
    private bool cachedLocallyOwned;
    private float syncTimer = Timers.ActorTimer;

    public long StationId
    {
        get
        {
            if (stationId != 0)
                return stationId;

            if (ranchDrone?._model != null)
                stationId = ranchDrone._model.StationId.Value;
            else if (explorerDrone?._model != null)
                stationId = explorerDrone._model.StationId.Value;

            return stationId;
        }
    }

    [UsedImplicitly]
    public void Awake()
    {
        ranchDrone = GetComponent<RanchDrone>();
        explorerDrone = GetComponent<ExplorerDrone>();
        regionMember = GetComponent<RegionMember>();
        rigidbody = GetComponent<Rigidbody>();

        if (explorerDrone != null)
            explorerMovement = GetComponentInChildren<ExplorerDroneMovementManager>(true);

        LocallyOwned = Main.Server.IsRunning;
        cachedLocallyOwned = LocallyOwned;

        if (Main.Server.IsRunning)
            CurrentOwnerId = Main.Server.PlayerId;
    }

    public void Start()
    {
        SetupHibernationEvent();

        if (Main.Client.IsConnected && !LocallyOwned && string.IsNullOrEmpty(CurrentOwnerId) && !IsHibernated)
            pendingInitialClaim = true;

        RegisterDrone();
    }

    private void RegisterDrone()
    {
        if (registered || StationId == 0)
            return;

        if (Drones.TryGetValue(StationId, out var existingStation) && existingStation && existingStation != this)
        {
            SrLogger.LogDebug($"Destroying duplicate drone for station {StationId}");

            HandlingPacket = true;
            try { Destroyer.DestroyAny(gameObject, "SR2MP.ReplaceDrone"); }
            catch { Destroy(gameObject); }
            HandlingPacket = false;

            return;
        }

        Drones[StationId] = this;
        registered = true;

        if (regionMember == null)
        {
            var stationModel = GetStationModel();
            var stationObj = stationModel?.GetGameObject();

            if (stationObj)
                regionMember = stationObj!.GetComponentInChildren<RegionMember>();

            SetupHibernationEvent();
        }

        if (NetworkDroneManager.PendingOwners.Remove(StationId, out var knownOwner))
        {
            CurrentOwnerId = knownOwner;
            LocallyOwned = knownOwner == LocalID;
            pendingInitialClaim = false;
        }
        else if (NetworkDroneManager.CachedOwners.TryGetValue(StationId, out var cachedOwner) &&
                 !string.IsNullOrEmpty(cachedOwner))
        {
            CurrentOwnerId = cachedOwner;
            LocallyOwned = cachedOwner == LocalID;
            pendingInitialClaim = false;
        }
        else if (pendingInitialClaim)
        {
            pendingInitialClaim = false;
            ClaimOwnership();
        }

        NetworkDroneManager.CachedOwners[StationId] = CurrentOwnerId;

        ApplyState(LocallyOwned);
        cachedLocallyOwned = LocallyOwned;
    }

    private void SetupHibernationEvent()
    {
        if (regionMember == null)
            return;

        try
        {
            regionMember.add_BeforeHibernationChanged(
                Il2CppSystem.Delegate.CreateDelegate(
                    Il2CppSystem.Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
                        .GetEvent("BeforeHibernationChanged").EventHandlerType,
                    Cast<Il2CppSystem.Object>(),
                    nameof(OnHibernationChanged),
                    true
                ).Cast<RegionMember.OnHibernationChange>()
            );
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to add drone hibernation event: {ex.Message}");
        }
    }

    public void OnHibernationChanged(bool hibernating)
    {
        IsHibernated = hibernating;

        if (DevMode)
            SrLogger.LogMessage($"Drone {StationId}: hibernation changed to {hibernating} (owner: '{CurrentOwnerId}', locally owned: {LocallyOwned})");

        if (hibernating)
        {
            var wasOwner = LocallyOwned;
            LocallyOwned = false;

            if (wasOwner)
            {
                SendUpdate(true);
                SendOwnershipPacket(string.Empty, CurrentOwnerId);
            }
        }
        else
        {
            ClaimOwnership();
        }
    }

    public void Update()
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected && !LocallyOwned)
            LocallyOwned = true;

        if (!registered)
        {
            RegisterDrone();
            if (!registered)
                return;
        }

        if (cachedLocallyOwned != LocallyOwned)
        {
            ApplyState(LocallyOwned);
            cachedLocallyOwned = LocallyOwned;
        }

        if (LocallyOwned)
        {
            syncTimer -= UnityEngine.Time.unscaledDeltaTime;
            if (syncTimer >= 0)
                return;

            syncTimer = Timers.ActorTimer;
            SendUpdate(false);
        }
        else
        {
            UpdateInterpolation();
        }
    }

    private void ApplyState(bool locallyOwned)
    {
        if (ranchDrone)
            ranchDrone!.enabled = locallyOwned;

        if (explorerDrone)
            explorerDrone!.enabled = locallyOwned;
        
        if (explorerMovement)
            explorerMovement!.enabled = locallyOwned;

        if (rigidbody)
            rigidbody!.constraints = locallyOwned ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
    }

    public void ClaimOwnership()
    {
        if (DevMode)
            SrLogger.LogMessage($"Drone {StationId}: claiming ownership (was '{CurrentOwnerId}')");

        LocallyOwned = true;
        CachedOwnerID = CurrentOwnerId;
        CurrentOwnerId = LocalID;
        NetworkDroneManager.CachedOwners[StationId] = LocalID;
        SendOwnershipPacket(LocalID, CachedOwnerID);
        SendUpdate(true);
    }

    private void SendOwnershipPacket(string claimerId, string previousOwnerId)
    {
        if (StationId == 0)
            return;
        
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        Main.SendToAllOrServer(new DroneOwnershipPacket
        {
            StationId = StationId,
            ClaimerID = claimerId,
            PreviousOwnerID = previousOwnerId
        });
    }

    [UsedImplicitly]
    public void OnDestroy()
    {
        if (registered && Drones.TryGetValue(stationId, out var drone) && drone == this)
            Drones.Remove(stationId);
    }
}
