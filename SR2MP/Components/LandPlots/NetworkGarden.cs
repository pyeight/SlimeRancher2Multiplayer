using Il2CppMonomiPark.SlimeRancher.Regions;
using JetBrains.Annotations;
using MelonLoader;
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
    private double cachedNextSpawnTime;

    private float syncTimer;
    private const float SyncInterval = 5f;

    [UsedImplicitly]
    public void Awake()
    {
        garden = GetComponent<SpawnResource>();
        regionMember = GetComponent<RegionMember>();

        if (garden != null && !string.IsNullOrEmpty(garden._id))
            Gardens[garden._id] = this;

        LocallyOwned = true;

        if (Main.Client.IsConnected)
            LocallyOwned = false;
    }

    public void Start()
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
        if (hibernating)
        {
            if (garden?._model != null)
                cachedNextSpawnTime = garden._model.nextSpawnTime;

            LocallyOwned = false;
        }
        else
        {
            if (garden?._model != null)
                garden._model.nextSpawnTime = cachedNextSpawnTime;

            LocallyOwned = true;

            if (garden == null || string.IsNullOrEmpty(garden._id))
                return;

            var packet = new GardenOwnershipPacket { GardenID = garden._id };
            Main.SendToAllOrServer(packet);
        }
    }

    public void Update()
    {
        if (!LocallyOwned || garden?._model == null)
            return;

        syncTimer += UnityEngine.Time.deltaTime;
        if (syncTimer < SyncInterval)
            return;

        syncTimer = 0;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        var packet = new GardenUpdatePacket
        {
            GardenID = garden._id,
            NextSpawnTime = garden._model.nextSpawnTime,
            StoredWater = garden._model.storedWater,
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

        garden._model.nextSpawnTime = nextSpawnTime;
        garden._model.storedWater = storedWater;
        garden._model.nextSpawnRipens = nextSpawnRipens;
    }
}