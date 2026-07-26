using Il2CppMonomiPark.SlimeRancher.Regions;
using JetBrains.Annotations;
using SR2MP.Packets.LandPlots;
using SR2MP.Shared.Managers;
using Starlight.Storage;

namespace SR2MP.Components.LandPlots;

[InjectIntoIL]
internal sealed class NetworkGarden : MonoBehaviour
{
    private SpawnResource? garden;
    private RegionMember regionMember;

    private bool registered;
    private int registerAttempts;
    private bool claimOnReady;
    private bool locallyOwned;
    private bool vanillaAllowSpawningInFastForwarding;
    private float lastActivityTime;

    private const float TakeoverDelayMin = 12f;
    private const float TakeoverDelayMax = 16f;
    private readonly float takeoverDelay = UnityEngine.Random.Range(TakeoverDelayMin, TakeoverDelayMax);

    private double cachedNextSpawnTime;
    private float cachedStoredWater;
    private bool cachedNextSpawnRipens;
    private bool hasCachedState;
    
    private string? resolvedId;
    internal string? Id
    {
        get
        {
            if (!string.IsNullOrEmpty(resolvedId))
                return resolvedId;

            var landPlot = GetComponentInParent<LandPlotLocation>();
            if (landPlot != null && !string.IsNullOrEmpty(landPlot._id) && garden != null)
            {
                var siblings = landPlot.GetComponentsInChildren<SpawnResource>(true);
                var gardenInstanceId = garden.GetInstanceID();
                var index = 0;
                for (var i = 0; i < siblings.Length; i++)
                {
                    if (siblings[i] != null && siblings[i].GetInstanceID() == gardenInstanceId)
                    {
                        index = i;
                        break;
                    }
                }
                resolvedId = $"{landPlot._id}_{index}";
            }
            else if (!string.IsNullOrEmpty(garden?._id))
                resolvedId = garden!._id;

            return resolvedId;
        }
    }

    public bool IsHibernated { get; private set; }
    private string CurrentOwnerId { get; set; } = string.Empty;

    public bool LocallyOwned
    {
        get => locallyOwned;
        private set
        {
            locallyOwned = value;
            ApplySimulationGate();
        }
    }

    private void ApplySimulationGate()
    {
        if (garden == null)
            return;

        garden.enabled = locallyOwned;
        garden._allowSpawningInFastForwarding = locallyOwned;
    }

    [UsedImplicitly]
    public void Awake()
    {
        garden = GetComponent<SpawnResource>();
        regionMember = GetComponentInParent<RegionMember>(true) ?? GetComponentInChildren<RegionMember>(true);

        if (garden != null)
            vanillaAllowSpawningInFastForwarding = garden._allowSpawningInFastForwarding;

        IsHibernated = false;
        lastActivityTime = UnityEngine.Time.time;

        if (Main.Server.IsRunning)
        {
            CurrentOwnerId = LocalID;
            LocallyOwned = true;
        }
        else
        {
            LocallyOwned = false;
        }
    }

    public void Start()
        => SetupHibernationEvent();

    public void Update()
    {
        NetworkGardenManager.Tick();

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        if (!registered)
        {
            TryRegister();
            return;
        }

        if (LocallyOwned || IsHibernated)
            return;

        if (UnityEngine.Time.time - lastActivityTime < takeoverDelay)
            return;
        
        SrLogger.LogGarden($"Garden '{Id}' silent for {takeoverDelay:F0}s, taking over");
        TakeOwnership();
    }
    private void TryRegister()
    {
        if (garden == null || string.IsNullOrEmpty(Id) || garden._model == null)
        {
            if (++registerAttempts == 60)
                SrLogger.LogGarden($"Garden register STUCK: name='{name}', id='{Id ?? "<null>"}', modelNull={garden?._model == null}");
            return;
        }

        registered = true;
        NetworkGardenManager.Register(this);
        ApplySimulationGate();

        SrLogger.LogGarden($"Garden registered: name='{name}', id='{Id}', locallyOwned={LocallyOwned}");

        if (claimOnReady)
        {
            claimOnReady = false;
            TakeOwnership();
        }
    }

    internal void TakeOwnership(bool broadcast = true)
    {
        CurrentOwnerId = LocalID;
        LocallyOwned = true;
        lastActivityTime = UnityEngine.Time.time;

        SrLogger.LogGarden($"Garden '{Id}' claimed by '{LocalID}' (broadcast={broadcast}, nextSpawnTime={garden?._model?.nextSpawnTime})");

        if (!broadcast)
            return;

        NetworkGardenManager.QueueClaim(Id!);
    }
    
    internal void SetOwner(string ownerId)
    {
        CurrentOwnerId = ownerId;
        LocallyOwned = ownerId == LocalID && !IsHibernated;
        lastActivityTime = UnityEngine.Time.time;

        SrLogger.LogGarden($"Garden '{Id}' owner set to '{ownerId}' (locallyOwned={LocallyOwned})");
    }

    internal void ClaimOnReady()
    {
        if (registered)
        {
            TakeOwnership();
            return;
        }

        claimOnReady = true;
    }
    
    internal void OnOwnerReleased()
    {
        CurrentOwnerId = string.Empty;

        if (!IsHibernated && registered)
        {
            SrLogger.LogGarden($"Garden '{Id}' released by owner, taking over");
            TakeOwnership();
        }
        else
        {
            SrLogger.LogGarden($"Garden '{Id}' released by owner, not taking over (hibernated={IsHibernated}, registered={registered})");
        }
    }

    internal void ResetToVanilla()
    {
        locallyOwned = false;
        CurrentOwnerId = string.Empty;

        if (garden != null)
        {
            garden.enabled = true;
            garden._allowSpawningInFastForwarding = vanillaAllowSpawningInFastForwarding;
        }
    }

    private void SetupHibernationEvent()
    {
        if (regionMember == null)
        {
            if (GardenLogging)
                SrLogger.LogWarning($"Garden '{Id}' has no RegionMember");
            return;
        }

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
            SrLogger.LogWarning($"NetworkGarden: Failed to add hibernation event: {ex.Message}");
        }
    }

    public void OnHibernationChanged(bool hibernating)
    {
        IsHibernated = hibernating;
        SrLogger.LogGarden($"Garden '{Id}' hibernation changed: hibernating={hibernating}, locallyOwned={LocallyOwned}");

        if (hibernating)
        {
            if (garden?._model != null)
                CacheState(garden._model.nextSpawnTime, garden._model.storedWater, garden._model.nextSpawnRipens);

            if (!LocallyOwned)
                return;

            NetworkGardenManager.QueueFinalState(this);
            LocallyOwned = false;

            // So others can take over immediately without having to wait the delay
            NetworkGardenManager.BroadcastRelease(Id!);
            SrLogger.LogGarden($"Garden '{Id}' released on hibernation");
            // CurrentOwnerId stays ours, if nobody claims it while we're away,
            // we resume simulation once its loaded again.
        }
        else
        {
            RestoreCachedState();
            lastActivityTime = UnityEngine.Time.time;

            if (CurrentOwnerId == LocalID)
            {
                LocallyOwned = true;
                SrLogger.LogGarden($"Garden '{Id}' ownership resumed on wake");
            }
        }
    }

    public void ApplyUpdate(double nextSpawnTime, float storedWater, bool nextSpawnRipens)
    {
        lastActivityTime = UnityEngine.Time.time;

        if (LocallyOwned)
            return;

        CacheState(nextSpawnTime, storedWater, nextSpawnRipens);

        if (garden?._model == null)
            return;

        garden._model.nextSpawnTime = nextSpawnTime;
        garden._model.storedWater = storedWater;
        garden._model.nextSpawnRipens = nextSpawnRipens;
    }

    internal GardenUpdatePacket.Entry? CaptureState()
    {
        if (garden?._model == null || string.IsNullOrEmpty(Id))
            return null;

        CacheState(garden._model.nextSpawnTime, garden._model.storedWater, garden._model.nextSpawnRipens);

        return new GardenUpdatePacket.Entry
        {
            GardenId = Id!,
            NextSpawnTime = garden._model.nextSpawnTime,
            StoredWater = garden._model.storedWater,
            NextSpawnRipens = garden._model.nextSpawnRipens
        };
    }

    private void CacheState(double nextSpawnTime, float storedWater, bool nextSpawnRipens)
    {
        cachedNextSpawnTime = nextSpawnTime;
        cachedStoredWater = storedWater;
        cachedNextSpawnRipens = nextSpawnRipens;
        hasCachedState = true;
    }

    public void RestoreCachedState()
    {
        if (!hasCachedState || garden?._model == null)
            return;

        garden._model.nextSpawnTime = cachedNextSpawnTime;
        garden._model.storedWater = cachedStoredWater;
        garden._model.nextSpawnRipens = cachedNextSpawnRipens;
    }

    [UsedImplicitly]
    public void OnDestroy()
        => NetworkGardenManager.Unregister(this);
}