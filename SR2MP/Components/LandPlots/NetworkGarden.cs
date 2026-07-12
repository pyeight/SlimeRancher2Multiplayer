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
    private float syncTimer;
    private const float SyncInterval = 5f;
    private bool cachedLocallyOwned;
    
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
            garden.enabled = LocallyOwned;

        if (Main.Server.IsRunning)
            CurrentOwnerId = Main.Server.PlayerId;
    }

    public void Start()
    {
        SetupHibernationEvent();

        if (Main.Client.IsConnected && !LocallyOwned && !IsHibernated)
            ClaimOwnership();
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
                cachedNextSpawnTime = garden._model.nextSpawnTime;
                
                if (previousOwner)
                    SendGardenUpdate();
            }

            LocallyOwned = false;

            if (previousOwner)
                SendOwnershipPacket(string.Empty, CurrentOwnerId);
        }
        else
        {
            if (garden?._model != null)
                garden._model.nextSpawnTime = cachedNextSpawnTime;

            ClaimOwnership();
        }
    }

    public void Update()
    {
        if (cachedLocallyOwned != LocallyOwned)
        {
            if (garden != null)
                garden.enabled = LocallyOwned;

            cachedLocallyOwned = LocallyOwned;
        }

        if (!LocallyOwned || garden?._model == null)
            return;

        syncTimer += UnityEngine.Time.deltaTime;
        if (syncTimer < SyncInterval)
            return;

        SendGardenUpdate();
    }

    private void SendGardenUpdate()
    {
        if (garden?._model == null)
            return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        syncTimer = 0;
        cachedNextSpawnTime = garden._model.nextSpawnTime;

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

        cachedNextSpawnTime = nextSpawnTime;
        
        garden._model.nextSpawnTime  = nextSpawnTime;
        garden._model.storedWater    = storedWater;
        garden._model.nextSpawnRipens = nextSpawnRipens;
    }

    public void ClaimOwnership()
    {
        LocallyOwned = true;
        CurrentOwnerId = LocalID;
        SendOwnershipPacket(LocalID, string.Empty);
        
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