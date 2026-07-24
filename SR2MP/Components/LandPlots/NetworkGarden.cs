using System.Collections;
using Il2CppMonomiPark.SlimeRancher.Regions;
using JetBrains.Annotations;
using SR2MP.Packets.LandPlots;
using Starlight.Storage;

namespace SR2MP.Components.LandPlots;

[InjectIntoIL]
internal sealed class NetworkGarden : MonoBehaviour
{
    private SpawnResource? garden;
    private RegionMember regionMember;

    public static readonly Dictionary<string, NetworkGarden> Gardens = new();

    public bool LocallyOwned { get; set; }
    public bool IsHibernated { get; private set; }
    public string CurrentOwnerId { get; set; } = string.Empty;

    private double cachedNextSpawnTime;
    private float cachedStoredWater;
    private bool cachedNextSpawnRipens;
    private bool hasCachedState;
    private float syncTimer;
    private const float SyncInterval = 5f;
    private bool cachedLocallyOwned;

    private float ownershipCheckTimer;
    private const float OwnershipCheckIntervalMin = 4f;
    private const float OwnershipCheckIntervalMax = 8f;
    private float ownershipCheckInterval = UnityEngine.Random.Range(OwnershipCheckIntervalMin, OwnershipCheckIntervalMax);
    
    internal static void OnServerStarted()
    {
        foreach (var spawnResource in FindObjectsOfType<SpawnResource>(true))
        {
            if (spawnResource.GetComponent<NetworkGarden>() == null)
                spawnResource.gameObject.AddComponent<NetworkGarden>();
        }

        foreach (var garden in Gardens.Values)
        {
            if (garden.IsHibernated)
                continue;

            garden.LocallyOwned = true;
            garden.CurrentOwnerId = Main.Server.PlayerId;

            SrLogger.LogDebug($"Garden '{garden.garden?._id}' assigned to host on server start (nextSpawnTime={garden.garden?._model?.nextSpawnTime})");
        }
    }

    [UsedImplicitly]
    public void Awake()
    {
        garden = GetComponent<SpawnResource>();
        regionMember = GetComponent<RegionMember>();

        if (garden != null && !string.IsNullOrEmpty(garden._id))
            Gardens[garden._id] = this;

        IsHibernated = false;
        LocallyOwned = Main.Server.IsRunning;
        cachedLocallyOwned = LocallyOwned;

        if (garden != null)
        {
            garden.enabled = LocallyOwned;
            garden._allowSpawningInFastForwarding = LocallyOwned;
        }

        if (Main.Server.IsRunning)
            CurrentOwnerId = Main.Server.PlayerId;
    }

    private bool IsReady => garden != null && !string.IsNullOrEmpty(garden._id) && garden._model != null;

    public void Start()
    {
        SetupHibernationEvent();

        if (Main.Client.IsConnected && !LocallyOwned && !IsHibernated)
            ContextShortcuts.StartCoroutine(ClaimWhenReady());
    }

    
    // This is not really good practise, but it works!
    // Todo: find a better way (if you wanna go through hell)
    private static readonly byte[] ClaimRetryFrameDelays = { 1, 2, 5, 10, 30 };

    private IEnumerator ClaimWhenReady()
    {
        foreach (var delay in ClaimRetryFrameDelays)
        {
            if (LocallyOwned || IsHibernated)
                yield break;

            if (IsReady)
            {
                ClaimOwnership();
                yield break;
            }

            yield return new WaitFrames(delay);
        }
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
            SrLogger.LogWarning($"Failed to add hibernation event: {ex.Message}");
        }
    }

    public void OnHibernationChanged(bool hibernating)
    {
        IsHibernated = hibernating;

        if (hibernating)
        {
            var previousOwner = LocallyOwned;

            if (garden?._model != null)
            {
                CacheState(garden._model.nextSpawnTime, garden._model.storedWater, garden._model.nextSpawnRipens);

                if (previousOwner)
                    SendGardenUpdate();
            }

            LocallyOwned = false;

            if (previousOwner)
                SendOwnershipPacket(string.Empty, CurrentOwnerId);
        }
        else
        {
            RestoreCachedState();

            if (IsUnowned())
                ClaimOwnership();
        }
    }

    public void Update()
    {
        if (cachedLocallyOwned != LocallyOwned)
        {
            if (garden != null)
            {
                garden.enabled = LocallyOwned;
                garden._allowSpawningInFastForwarding = LocallyOwned;
            }

            cachedLocallyOwned = LocallyOwned;
        }

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        if (!LocallyOwned && !IsHibernated)
        {
            ownershipCheckTimer += UnityEngine.Time.deltaTime;
            if (ownershipCheckTimer >= ownershipCheckInterval)
            {
                ownershipCheckTimer = 0f;
                ownershipCheckInterval = UnityEngine.Random.Range(OwnershipCheckIntervalMin, OwnershipCheckIntervalMax);
                TryOwnUnowned();
            }
        }
        else
        {
            ownershipCheckTimer = 0f;
        }

        if (!LocallyOwned || garden?._model == null)
            return;

        syncTimer += UnityEngine.Time.deltaTime;
        if (syncTimer < SyncInterval)
            return;

        SendGardenUpdate();
    }
    
    internal static void ReassignOwnership()
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
        
        foreach (var garden in Gardens.Values)
        {
            if (!garden || garden.IsHibernated)
                continue;

            garden.CurrentOwnerId = string.Empty;
            garden.LocallyOwned = false;
        }

        foreach (var (gardenId, garden) in Gardens.ToArray())
        {
            try
            {
                if (!garden || garden.IsHibernated)
                    continue;

                var position = garden.transform.position;
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

                garden.CurrentOwnerId = newOwner;
                garden.LocallyOwned = newOwner == LocalID;

                SrLogger.LogDebug($"Garden '{gardenId}' reassigned to '{newOwner}' after reassignment");

                Main.SendToAllOrServer(new GardenOwnershipPacket
                {
                    GardenID = gardenId,
                    ClaimerID = newOwner,
                    PreviousOwnerID = string.Empty
                });
            }
            catch { /* ignored */ }
        }
    }

    private bool IsUnowned()
        => string.IsNullOrEmpty(CurrentOwnerId) || CurrentOwnerId == LocalID || !PlayerManager.CheckPlayerExists(CurrentOwnerId);

    private void TryOwnUnowned()
    {
        if (!IsUnowned())
            return;

        SrLogger.LogDebug($"Garden '{garden?._id}' reclaimed from '{CurrentOwnerId}' (nextSpawnTime={garden?._model?.nextSpawnTime})");
        ClaimOwnership();
    }

    private void SendGardenUpdate()
    {
        if (!IsReady)
            return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        syncTimer = 0;
        CacheState(garden._model.nextSpawnTime, garden._model.storedWater, garden._model.nextSpawnRipens);

        var packet = new GardenUpdatePacket
        {
            GardenID       = garden._id,
            NextSpawnTime  = garden._model.nextSpawnTime,
            StoredWater    = garden._model.storedWater,
            NextSpawnRipens = garden._model.nextSpawnRipens
        };
        Main.SendToAllOrServer(packet);
    }

    [UsedImplicitly]
    public void OnDestroy()
    {
        if (garden != null && !string.IsNullOrEmpty(garden._id))
            Gardens.Remove(garden._id);
    }

    public void ApplyUpdate(double nextSpawnTime, float storedWater, bool nextSpawnRipens)
    {
        if (garden?._model == null)
            return;

        CacheState(nextSpawnTime, storedWater, nextSpawnRipens);

        garden._model.nextSpawnTime  = nextSpawnTime;
        garden._model.storedWater    = storedWater;
        garden._model.nextSpawnRipens = nextSpawnRipens;
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
    
    internal static void RestoreAfterTimeSkip()
    {
        foreach (var garden in Gardens.Values)
        {
            if (garden.LocallyOwned)
                continue;

            garden.RestoreCachedState();
        }
    }
    
    internal static void OnDisconnected()
    {
        foreach (var garden in Gardens.Values)
            garden.LocallyOwned = false;
    }

    public void ClaimOwnership()
    {
        if (!IsReady)
            return;

        LocallyOwned = true;
        CurrentOwnerId = LocalID;
        SendOwnershipPacket(LocalID, string.Empty);

        SrLogger.LogDebug($"Garden '{garden?._id}' claimed by '{LocalID}' (nextSpawnTime={garden?._model?.nextSpawnTime}, storedWater={garden?._model?.storedWater})");

        SendGardenUpdate();
    }

    private void SendOwnershipPacket(string claimerId, string previousOwnerId)
    {
        if (garden == null || string.IsNullOrEmpty(garden._id))
            return;
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        Main.SendToAllOrServer(new GardenOwnershipPacket
        {
            GardenID        = garden._id,
            ClaimerID       = claimerId,
            PreviousOwnerID = previousOwnerId
        });
    }
}